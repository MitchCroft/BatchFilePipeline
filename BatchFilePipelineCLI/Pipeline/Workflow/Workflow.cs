using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using System.Diagnostics;

namespace BatchFilePipelineCLI.Pipeline.Workflow
{
    /// <summary>
    /// Handle the initialisation and execution of a workflow based on the supplied description
    /// </summary>
    internal sealed class Workflow
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Store the collection of graphs that will be executed as a part of the workflow
        /// </summary>
        private readonly List<IGraphProcess> _workflowGraphs = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Try to parse the description of values as a valid workflow that can be executed
        /// </summary>
        /// <param name="workflowDescription">A description of the workflow that is to be processed</param>
        /// <param name="library">The library of available Nodes that can be used for processing</param>
        /// <param name="environmentVariables">The collection of environment variables that have been created for use</param>
        /// <param name="argumentVariables">The collection of argument variables that were passed in to the program as override behaviour flags</param>
        /// <returns>Returns true if the workflow was able to be created for use</returns>
        public bool TryLoadFromDescription(WorkflowDescription workflowDescription,
                                           NodeLibrary library,
                                           Dictionary<string, string?> environmentVariables,
                                           Dictionary<string, string?> argumentVariables)
        {
            // The workflow description will have the different graphs that we need to create and process
            (GraphDescription graphDescription, IGraphProcess processor)[] graphProcessors =
            {
                (workflowDescription.PreProcessGraph, new GenericGraphProcess(NodeUsage.PreProcess)),
                (workflowDescription.ProcessGraph, new GenericGraphProcess(NodeUsage.Process)),
                (workflowDescription.PostProcessGraph, new GenericGraphProcess(NodeUsage.PostProcess))
            };

            // Try to create all of the graphs that are needed for processing
            bool loadSuccess = true;
            _workflowGraphs.Clear();
            for (int i = 0; i < graphProcessors.Length; ++i)
            {
                // Try to load the description for the processor that is needed
                var (graphDescription, processor) = graphProcessors[i];
                if (processor.TryLoadFromDescription(graphDescription, library, environmentVariables, argumentVariables) == false)
                {
                    Logger.Error($"Failed to load the graph at index {i} for the workflow");
                    loadSuccess = false;
                    continue;
                }

                // The graph is valid for use
                _workflowGraphs.Add(processor);
            }
            return loadSuccess;
        }

        /// <summary>
        /// Start the process of executing the workflow graphs in sequence to completion
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that will be used to control the running of the graph</param>
        /// <returns>Returns the status code of the operation once it has finished running</returns>
        public async ValueTask<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            // We are going to build up a collection of runtime variables that will be shared between the different stages for processing
            Dictionary<string, object?> runtimeVariables = new();

            // We can run each graph in sequence, each one should lead into the next
            foreach (var processor in _workflowGraphs)
            {
                // Start the execution process
                Logger.Log($"Beginning execution of {processor}...");
                var stopwatch = Stopwatch.StartNew();
                var result = await processor.EvaluateGraphAsync(runtimeVariables, cancellationToken);
                stopwatch.Stop();

                // If the operation was cancelled, then we can exit early
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.Warning($"Execution of {processor} was cancelled after {stopwatch.Elapsed}");
                    return 499;
                }

                // If this process failed, then we have a problem
                if (result.IsError == true)
                {
                    Logger.Error($"[{nameof(Workflow)}] Encountered an error when processing '{processor}' after {stopwatch.Elapsed}\n{result}");
                    return result.ResultCode;
                }

                // This processor passed successfully
                Logger.Success($"[{nameof(Workflow)}] Successfully processed '{processor}' after {stopwatch.Elapsed}");
                Logger.Log($"[{nameof(Workflow)}] {result}{(result.Results == null || result.Results.Count == 0 ? string.Empty : $"\n\tReceived exported values:\n\t\t{string.Join("\n\t\t", result.Results.Select(x => $"{x.Key}={x.Value}"))}")}");

                // Check if there are any new runtime variables that need to be added to the list
                if (result.Results != null)
                {
                    foreach (var (key, value) in result.Results)
                    {
                        runtimeVariables[key] = value;
                    }
                }
            }

            // If we made it this far, then we're good
            return 0;
        }
    }
}
