using BatchFilePipelineCLI.DynamicProperties;
using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using BatchFilePipelineCLI.Utility.Cancellation;
using BatchFilePipelineCLI.Utility.Extensions;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Handle the running of the graph elements that are needed to process the workflow element
    /// </summary>
    internal sealed class MainGraphProcess : IGraphProcess
    {
        /*----------Variables----------*/
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
        private IDictionary<string, string?>? _environmentVariables;

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
                                           IDictionary<string, string?> environmentVariables,
                                           IDictionary<string, string?> argumentVariables)
        {
            // We need to of been given a main graph description object that we can use for processing
            if (description is not MainGraphDescription graphDescription)
            {
                throw new ArgumentException($"[{nameof(MainGraphProcess)}] Invalid graph description object received '{description}'");
            }

            // Grab the environment variables that can be used
            _environmentVariables = environmentVariables.Merge(graphDescription.Environment ?? new KeyValueSection(), argumentVariables);

            // Try to load the different graphs that are needed for processing
            return GraphRunnerBuilder.TryBuildGraphRunner(graphDescription.IdentificationGraph, library, NodeUsage.Identification, environmentVariables, argumentVariables, out _identificationGraphRunner) &&
                GraphRunnerBuilder.TryBuildGraphRunner(graphDescription.ProcessGraph, library, NodeUsage.Process, environmentVariables, argumentVariables, out _processGraphRunner);
        }

        /// <summary>
        /// Handle the process of evaluating the defined graph with the specified values
        /// </summary>
        /// <param name="runtimeVariables">A collection of existing runtime variables that can be used for processing</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the result of the execution process</returns>
        public async ValueTask<ExecutionResult> EvaluateGraphAsync(IDictionary<string, object?> runtimeVariables,
                                                                   CancellationToken cancellationToken)
        {
            // We need to have the graphs built for use
            if (_identificationGraphRunner == null ||
                _processGraphRunner == null)
            {
                throw new NullReferenceException($"[{nameof(MainGraphProcess)}] Unexpected null value for the graph being processed");
            }

            // Look for the properties that will define how we operate this process
            if (ArgumentResolver.TryResolveEnvironmentVariable(_watchProperty, _environmentVariables!, out bool watchFiles) == false)
            {
                Logger.Warning($"[{nameof(MainGraphProcess)}] Unable to resolve the environment variable '{_watchProperty}'");
            }
            if (ArgumentResolver.TryResolveEnvironmentVariable(_sleepPeriodProperty, _environmentVariables!, out int sleepPeriod) == false)
            {
                sleepPeriod = (int)_sleepPeriodProperty.DefaultValue!;
                Logger.Warning($"[{nameof(MainGraphProcess)}] Unabel to resolve the environment variable '{_sleepPeriodProperty}'");
            }
            if (ArgumentResolver.TryResolveEnvironmentVariable(_propergateFailureProperty, _environmentVariables!, out bool propergateFailure) == false)
            {
                Logger.Warning($"[{nameof(MainGraphProcess)}] Unabel to resolve the environment variable '{_propergateFailureProperty}'");
            }

            // There may be values that we need to export from this graph
            Dictionary<string, object?> outputResults = new Dictionary<string, object?>();

            // We will manage this process as a separate, cancellable task that won't effect the flow of everything else
            using (var token = CancellationStack.PushSource())
            {
                do
                {
                    // Process the graph that will be used to identify files that are needed for processing
                    var idOutput = await _identificationGraphRunner.ExecuteGraphAsync(runtimeVariables, token);
                    if (token.IsCancellationRequested == true)
                    {
                        return new ExecutionResult(outputResults);
                    }

                    // We're expecting a collection of elements that can be used as the inputs for the process
                    if (idOutput.IsError == true)
                    {
                        return idOutput;
                    }

                    // We are expecting an enumerable collection of strings as the identifiers of the files that need to be processed
                    var identifiers = idOutput.Results?.Where(x => x.Value is IEnumerable<string>)
                        .SelectMany(x => (IEnumerable<string>)x.Value!)
                        .ToArray();

                    // If there are no identifiers, nothing we can do
                    if (identifiers == null)
                    {
                        Logger.Error($"[{nameof(MainGraphProcess)}] No identifier values were emitted from the identification stage, was expecting the output of IEnumerable<string> value for processing");
                    }

                    // We have a set of the elements that need to be processed
                    else
                    {
                        foreach (var id in identifiers)
                        {
                            // Try to handle the processing of the identified elements
                            try
                            {
                                // We're going to need a new set of runtime variables for this entry
                                Dictionary<string, object?> instancedRuntimeVariables = new Dictionary<string, object?>(runtimeVariables);

                                // Set the identifier that will be available for use in the process
                                instancedRuntimeVariables["CurrentIdentifier"] = id;

                                // Process the graph for the identifier
                                var processOutput = await _processGraphRunner.ExecuteGraphAsync(instancedRuntimeVariables, token);
                                if (token.IsCancellationRequested == true)
                                {
                                    return new ExecutionResult(outputResults);
                                }
                                if (processOutput.IsError == true)
                                {
                                    Logger.Error($"[{nameof(MainGraphProcess)}] Encountered an error while processing '{id}'\n{processOutput}");
                                    if (propergateFailure == true)
                                    {
                                        return processOutput;
                                    }
                                    continue;
                                }

                                Logger.Success($"[{nameof(MainGraphProcess)}] Processed: '{id}'\n{processOutput}");

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
            }

            // If we got this far, we're good
            return new ExecutionResult(outputResults);
        }
    }
}
