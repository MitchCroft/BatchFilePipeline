using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using BatchFilePipelineCLI.Utility.Cancellation;
using BatchFilePipelineCLI.Utility.ExecutionState;
using BatchFilePipelineCLI.Utility.Extensions;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Handle the running of the graph elements that are needed to process the workflow element
    /// </summary>
    internal sealed class MainGraphProcess : IGraphProcess
    {
        /*----------Variables----------*/
        //CONST

        /// <summary>
        /// The name of the export property that is expected when processing the identification graph
        /// </summary>
        private const string IDENTIFIER_OUTPUT_PROPERTY = "Identifiers";

        /// <summary>
        /// The name that the current identifier value will be stored under for processing
        /// </summary>
        private const string CURRENT_IDENTIFIER_PROPERTY = "CurrentIdentifier";

        /// <summary>
        /// The label that will be assigned to the property used to display the run summary of the operation
        /// </summary>
        private const string RUN_SUMMARY_PROPERTY = "RunSummary";

        //PRIVATE

        /// <summary>
        /// Define a property that can be used to flag if changes to the available files should be waited for when processing
        /// </summary>
        private readonly Property _watchProperty = Property.Create
        (
            "watch",
            "Flag that indicates if changes to identified files should be waited for when processing the workflow",
            defaultValue: false
        );

        /// <summary>
        /// Define the length of time that will be waited between attempts to look for new files when watching the identified files
        /// </summary>
        private readonly Property _sleepPeriodProperty = Property.Create
        (
            "sleepInterval",
            "The length of time (in milliseconds) between attempts to find new files that can be processed in the workflow",
            defaultValue: 60000,
            example: "1000 = 1 second"
        );

        /// <summary>
        /// Flags if the entire process should be failed if one of the processed files fails
        /// </summary>
        private readonly Property _propergateFailureProperty = Property.Create
        (
            "propergateFailure",
            "Flags if an error occurring during the processing of an identified file should bubble to the top",
            defaultValue: false
        );

        /// <summary>
        /// The collection of ID'd runners that can be used as sub-processes for a collection of data within the graph
        /// </summary>
        private readonly Dictionary<string, GraphRunner> _subProcessGraphRunners = new();

        /// <summary>
        /// The runner that will be used to perform the file identification process
        /// </summary>
        private GraphRunner? _identificationGraphRunner;

        /// <summary>
        /// The runner that will be used to perform the file processing actions
        /// </summary>
        private GraphRunner? _processGraphRunner;

        /// <summary>
        /// The collection of environment variabels that have been defined for this process
        /// </summary>
        private IReadOnlyDictionary<string, string?>? _environmentVariables;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Attempt to load the graph object with the given description of nodes that need to be processed
        /// </summary>
        /// <param name="description">The description of the graph that is to be processed</param>
        /// <param name="library">The library of nodes that are available for use</param>
        /// <param name="environmentVariables">The collection of environment variables that are available for use</param>
        /// <param name="argumentVariables">The collection of command line argument variables that have been supplied to the program for use</param>
        /// <returns>Returns true if the graph process could be loaded properly for use</returns>
        public bool TryLoadFromDescription(IGraphDescription description,
                                           NodeLibrary library,
                                           IReadOnlyDictionary<string, string?> environmentVariables,
                                           IReadOnlyDictionary<string, string?> argumentVariables)
        {
            // We need to of been given a main graph description object that we can use for processing
            if (description is not MainGraphDescription graphDescription)
            {
                throw new ArgumentException($"[{nameof(MainGraphProcess)}] Invalid graph description object received '{description}'");
            }

            // Grab the environment variables that can be used
            _environmentVariables = environmentVariables.Merge(graphDescription.Environment ?? new KeyValueSection(), argumentVariables);

            // Attempt to create the main graph sections for proecssing results
            if (GraphRunnerBuilder.TryBuildGraphRunner(graphDescription.IdentificationGraph, library, NodeUsage.Identification, environmentVariables, argumentVariables, out _identificationGraphRunner) == false ||
                GraphRunnerBuilder.TryBuildGraphRunner(graphDescription.ProcessGraph, library, NodeUsage.Process, environmentVariables, argumentVariables, out _processGraphRunner) == false)
            {
                return false;
            }

            // Identify all of the sub-processes that are defined and create runners for their definitions
            _subProcessGraphRunners.Clear();
            for (int i = 0; i < graphDescription.SubProcessGraphs?.Length; ++i)
            {
                // There must be an ID associated with this graph so it can be referenced
                var subProcGraph = graphDescription.SubProcessGraphs[i];
                if (string.IsNullOrWhiteSpace(subProcGraph.Id) == true)
                {
                    Logger.Error($"[{nameof(MainGraphProcess)}] Failed to build sub-process graph '{i}'{(string.IsNullOrEmpty(subProcGraph.Name) ? $" ({subProcGraph.Name})" : string.Empty)} as there is no ID defined");
                    return false;
                }

                // If the ID is already in use, that's another problem
                if (_subProcessGraphRunners.TryGetValue(subProcGraph.Id, out var prevSubProcGraph) == true)
                {
                    Logger.Error($"[{nameof(MainGraphProcess)}] Failed to build build sub-process graph '{i}'{(string.IsNullOrEmpty(subProcGraph.Name) ? $" ({subProcGraph.Name})" : string.Empty)}, as the ID '{subProcGraph.Id}' is already in use");
                    return false;
                }

                // Try to build the graph runner for use
                if (GraphRunnerBuilder.TryBuildGraphRunner(subProcGraph, library, NodeUsage.Process, environmentVariables, argumentVariables, out var subProcessGraphRunner) == false)
                {
                    return false;
                }

                // We've got the sub-process graph that can be used for processing data
                _subProcessGraphRunners[subProcGraph.Id] = subProcessGraphRunner;
            }

            // If we got this far, we're good
            return true;
        }

        /// <summary>
        /// Handle the process of evaluating the defined graph with the specified values
        /// </summary>
        /// <param name="runtimeVariables">A collection of existing runtime variables that can be used for processing</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the result of the execution process</returns>
        public async ValueTask<ExecutionResult> EvaluateGraphAsync(IReadOnlyDictionary<string, object?> runtimeVariables,
                                                                   CancellationToken cancellationToken)
        {
            // We need to have the graphs built for use
            if (_identificationGraphRunner == null ||
                _processGraphRunner == null)
            {
                throw new NullReferenceException($"[{nameof(MainGraphProcess)}] Unexpected null value for the graph being processed");
            }

            // Look for the properties that will define how we operate this process
            if (Resolver.TryResolveEnvironmentVariable(_watchProperty, _environmentVariables!, out bool watchFiles) == false)
            {
                Logger.Warning($"[{nameof(MainGraphProcess)}] Unable to resolve the environment variable '{_watchProperty}'");
            }
            if (Resolver.TryResolveEnvironmentVariable(_sleepPeriodProperty, _environmentVariables!, out int sleepPeriod) == false)
            {
                sleepPeriod = (int)_sleepPeriodProperty.DefaultValue!;
                Logger.Warning($"[{nameof(MainGraphProcess)}] Unable to resolve the environment variable '{_sleepPeriodProperty}'");
            }
            if (Resolver.TryResolveEnvironmentVariable(_propergateFailureProperty, _environmentVariables!, out bool propergateFailure) == false)
            {
                Logger.Warning($"[{nameof(MainGraphProcess)}] Unable to resolve the environment variable '{_propergateFailureProperty}'");
            }

            // There may be values that we need to export from this graph
            Dictionary<string, object?> outputResults = new Dictionary<string, object?>();

            // Store a list of the elements that were processed
            HashSet<object> identifiedElements = new();
            List<object> completedSuccessfully = new();
            List<object> completedFailure = new();

            // We will manage this process as a separate, cancellable task that won't effect the flow of everything else
            bool wasCancelled = false;
            ExecutionResult errorResult = default;
            DateTime startTime = DateTime.Now;
            using (var token = CancellationStack.PushSource(cancellationToken))
            {
                do
                {
                    // Process the graph that will be used to identify files that are needed for processing
                    var idOutput = await _identificationGraphRunner.ExecuteGraphAsync(runtimeVariables, null, token);
                    if (token.IsCancellationRequested == true)
                    {
                        break;
                    }

                    // We're expecting a collection of elements that can be used as the inputs for the process
                    if (idOutput.IsError == true)
                    {
                        return idOutput;
                    }

                    Logger.Success($"[{nameof(MainGraphProcess)}] Finished running identification step");
                    Logger.Log($"[{nameof(MainGraphProcess)}] {idOutput}{(idOutput.Results == null || idOutput.Results.Count == 0 ? string.Empty : $"\n\tReceived exported values:\n\t\t{string.Join("\n\t\t", idOutput.Results.Select(x => $"{x.Key}={x.Value}"))}")}");

                    // We're expecting to get a collection of elements in the specified output
                    if (idOutput.Results == null ||
                        idOutput.Results.TryGetValue(IDENTIFIER_OUTPUT_PROPERTY, out var outputIdentifiers) == false ||
                        outputIdentifiers is not IEnumerable identifiers)
                    {
                        Logger.Error($"[{nameof(MainGraphProcess)}] Expected the identifier graph to emit a collection of values under the '{IDENTIFIER_OUTPUT_PROPERTY}' property name");
                    }

                    // We have a set of the elements that need to be processed
                    else
                    {
                        // We want to keep the PC running while this process is running
                        using var stateMarker = ExecutionStateHandler.Push();

                        // Handle all of the identifiers that were identified
                        foreach (var id in identifiers)
                        {
                            // Try to handle the processing of the identified elements
                            try
                            {
                                // We're going to need a new set of runtime variables for this entry
                                Logger.Log($"[{nameof(MainGraphProcess)}] Starting to process '{id}'");
                                Dictionary<string, object?> instancedRuntimeVariables = runtimeVariables.Merge(idOutput.Results);

                                // Set the identifier that will be available for use in the process
                                instancedRuntimeVariables[CURRENT_IDENTIFIER_PROPERTY] = id;

                                // Process the graph for the identifier
                                var processOutput = await _processGraphRunner.ExecuteGraphAsync(instancedRuntimeVariables, TryGetSubProcessById, token);
                                if (token.IsCancellationRequested == true)
                                {
                                    break;
                                }
                                if (processOutput.IsError == true)
                                {
                                    completedFailure.Add(id);
                                    Logger.Error($"[{nameof(MainGraphProcess)}] Encountered an error while processing '{id}'\n{processOutput}");
                                    if (propergateFailure == true)
                                    {
                                        return processOutput;
                                    }
                                    errorResult = processOutput;
                                    continue;
                                }

                                completedSuccessfully.Add(id);
                                Logger.Success($"[{nameof(MainGraphProcess)}] Processed: '{id}'");
                                Logger.Log($"[{nameof(MainGraphProcess)}] {processOutput}{(processOutput.Results == null || processOutput.Results.Count == 0 ? string.Empty : $"\n\tReceived exported values:\n\t\t{string.Join("\n\t\t", processOutput.Results.Select(x => $"{x.Key}={x.Value}"))}")}");

                                // If there were output values, add them to the output
                                if (processOutput.Results != null)
                                {
                                    foreach (var (key, value) in processOutput.Results)
                                    {
                                        outputResults[key] = value;
                                    }
                                }
                            }

                            // Anything going wrong is going to be a problem
                            catch (Exception ex)
                            {
                                return new ExecutionResult(ex);
                            }
                        }
                    }

                    // If we're watching for file changes, we can sleep
                    if (watchFiles == true)
                    {
                        await Task.Delay(sleepPeriod, token)
                            .SurpressCancellation();
                    }

                } while (watchFiles == true && token.IsCancellationRequested == false);
                wasCancelled = token.IsCancellationRequested;
            }

            // Generate the summary object that will be exported
            outputResults[RUN_SUMMARY_PROPERTY] = new MainProcessSummary
            (
                wasCancelled == true ? -1 : completedFailure.Count == 0 ? 0 : errorResult.ResultCode,
                wasCancelled == true ? "Cancelled" : completedFailure.Count == 0 ? "Success" : errorResult.DetailMessage,
                DateTime.Now - startTime,
                completedSuccessfully.ToArray(),
                completedFailure.ToArray()
            );

            int total = completedSuccessfully.Count + completedFailure.Count;
            if (total > 0)
            {
                Logger.Log($"[{nameof(MainGraphProcess)}]==================== RUN SUMMARY ====================");
                if (completedSuccessfully.Count > 0)
                {
                    float successRate = completedSuccessfully.Count / (float)total;
                    Logger.Success($"Success {completedSuccessfully.Count}/{total} ({successRate:P})\n\t{string.Join("\n\t", completedSuccessfully)}");
                }
                if (completedFailure.Count > 0)
                {
                    float failureRate = completedFailure.Count / (float)total; 
                    Logger.Error($"Failed {completedFailure.Count}/{total} ({failureRate:P})\n\t{string.Join("\n\t", completedFailure)}");
                }
                Logger.Log("=====================================================");
            }
            else
            {
                Logger.Log($"[{nameof(MainGraphProcess)}] Unable to find any files to process");
            }

            // If we got this far, we're good
            return new ExecutionResult(outputResults);
        }

        /// <summary>
        /// Use the name of the type as the string description
        /// </summary>
        public override string ToString() => nameof(MainGraphProcess);

        //PRIVATE

        /// <summary>
        /// Try to get a sub-process graph runner by the given ID, which can be used for processing a collection of data within the graph
        /// </summary>
        /// <param name="id">The ID of the graph to be retrieved</param>
        /// <returns>Returns the graph runner or null if none could be found for the ID</returns>
        private GraphRunner? TryGetSubProcessById(string id) =>
            _subProcessGraphRunners.TryGetValue(id, out var graphRunner) == true ? graphRunner : null;
    }
}
