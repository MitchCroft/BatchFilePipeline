using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
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
        private readonly List<RuntimeGraph> _workflowGraphs = new List<RuntimeGraph>();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Try to parse the description of values as a valid workflow that can be executed
        /// </summary>
        /// <param name="description">A description of the workflow that is to be processed</param>
        /// <param name="library">The library of available Nodes that can be used for processing</param>
        /// <param name="environmentVariables">The collection of environment variables that have been created for use</param>
        /// <param name="argumentVariables">The collection of argument variables that were passed in to the program as override behaviour flags</param>
        /// <returns>Returns true if the workflow was able to be created for use</returns>
        public bool TryLoadFromDescription(WorkflowDescription description,
                                           NodeLibrary library,
                                           Dictionary<string, string?> environmentVariables,
                                           Dictionary<string, string?> argumentVariables)
        {
            // We need to create instances of each graph that can be used for processing
            GraphFactoryMethod[] graphFactories = {
                (out RuntimeGraph runtimeGraph) => TryCreateGraphFromDescription<PreProcessSupportGraph>
                (
                    description.PreProcessGraph,
                    library,
                    environmentVariables,
                    argumentVariables,
                    out runtimeGraph
                ),
                (out RuntimeGraph runtimeGraph) => TryCreateGraphFromDescription<MainProcessGraph>
                (
                    description.ProcessGraph,
                    library,
                    environmentVariables,
                    argumentVariables,
                    out runtimeGraph
                ),
                (out RuntimeGraph runtimeGraph) => TryCreateGraphFromDescription<PostProcessSupportGraph>
                (
                    description.PostProcessGraph,
                    library,
                    environmentVariables,
                    argumentVariables,
                    out runtimeGraph
                )
            };

            // Try to create all of the graphs that are needed for processing
            bool loadSuccess = true;
            _workflowGraphs.Clear();
            for (int i = 0; i < graphFactories.Length; ++i)
            {
                // Try to create the graph instance
                if (graphFactories[i](out RuntimeGraph runtimeGraph) == false)
                {
                    Logger.Error($"Failed to create graph {i} for the workflow");
                    loadSuccess = false;
                    continue;
                }

                // The graph is valid for use
                _workflowGraphs.Add(runtimeGraph);
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
            // We can run each graph in sequence, each one should lead into the next
            for (int i = 0; i < _workflowGraphs.Count; ++i)
            {
                // We've got the graph that will be processed
                var graph = _workflowGraphs[i].Graph;
                var environmentVariables = _workflowGraphs[i].EnvironmentVariables;

                // Start the execution process
                Logger.Log($"Beginning execution of the {graph.Name} graph...");
                var stopwatch = Stopwatch.StartNew();
                int result = await graph.EvaluateGraphAsync(environmentVariables, cancellationToken);
                stopwatch.Stop();

                // If the operation was cancelled, then we can exit early
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.Warning($"Execution of the {graph.Name} graph was cancelled after {stopwatch.Elapsed}");
                    return 499;
                }

                // Determine the result of the graph execution
                if (result != 0)
                {
                    Logger.Error($"Execution of the {graph.Name} graph failed with result code {result} after {stopwatch.Elapsed}");
                    return result;
                }

                // Otherwise we've completed successfully
                Logger.Log($"Completed execution of the {graph.Name} graph successfully in {stopwatch.Elapsed}");
            }

            // If we made it this far, then we're good
            return 0;
        }

        //PRIVATE

        /// <summary>
        /// Attempt to create a graph from the supplied description
        /// </summary>
        /// <typeparam name="T">The type of graph that will be created</typeparam>
        /// <param name="graphDescription">The description of the graph that will be created</param>
        /// <param name="library">The library of available Nodes that will be used in processing</param>
        /// <param name="environmentVariables">The collection of environment variables that are available as a baseline</param>
        /// <param name="argumentVariables">The collection of argument variables that were passed in to the program as override behaviour flags</param>
        /// <param name="runtimeGraph">Passes out the valid instance of the graph if it was able to be created</param>
        /// <returns>Returns true if the graph could be created successfully from the description</returns>
        private static bool TryCreateGraphFromDescription<T>(GraphDescription graphDescription,
                                                             NodeLibrary library,
                                                             Dictionary<string, string?> environmentVariables,
                                                             Dictionary<string, string?> argumentVariables,
                                                             out RuntimeGraph runtimeGraph)
            where T : ProcessGraph, new()
        {
            // Create the graph instance that will be populated later on
            var graphInstance = new T();

            // Retrieve the instanced environment variables for this graph
            var graphEnvironmentVariables = environmentVariables
                .Concat(graphDescription.Environment)
                .Concat(argumentVariables)
                .ToDictionary(x => x.Key, x => x.Value);
            Logger.Log($"{graphInstance.Name} Graph Environment Variable Set ({graphEnvironmentVariables.Count}):\n\t{string.Join("\n\t", graphEnvironmentVariables.Select((v, i) => $"{i}.\t{v.Key}={v.Value}"))}");

            // Try to initialise the graph with the supplied description
            if (graphInstance.TryInitialiseGraph(graphDescription, library, graphEnvironmentVariables) == false)
            {
                Logger.Error($"Failed to initialise the {graphInstance.Name} graph from the supplied description");
                runtimeGraph = default;
                return false;
            }

            // We have the elements that can be processed
            runtimeGraph = new RuntimeGraph(graphInstance, graphEnvironmentVariables);
            return true;
        }

        /*----------Types----------*/
        //PRIVATE

        /// <summary>
        /// Delegate for methods that can create graphs from descriptions
        /// </summary>
        /// <param name="graphDescription">The description of the graph that will be created</param>
        /// <param name="library">The library of available Nodes that will be used in processing</param>
        /// <param name="environmentVariables">The collection of environment variables that are available as a baseline</param>
        /// <param name="argumentVariables">The collection of argument variables that were passed in to the program as override behaviour flags</param>
        /// <param name="runtimeGraph">Passes out the valid instance of the graph if it was able to be created</param>
        /// <returns>Returns true if the graph could be created successfully from the description</returns>
        private delegate bool GraphFactoryMethod(out RuntimeGraph runtimeGraph);

        /// <summary>
        /// Cache the instanec of the runtime graphs that will be used for processing
        /// </summary>
        private readonly struct RuntimeGraph
        {
            /*----------Variables----------*/
            //PUBLIC

            /// <summary>
            /// The instance of the graph that can be used for processing
            /// </summary>
            public readonly ProcessGraph Graph;

            /// <summary>
            /// The collection of graph-specific environment variables that should be used while processing
            /// </summary>
            public readonly Dictionary<string, string?> EnvironmentVariables;

            /*----------Functions----------*/
            //PUBLIC

            /// <summary>
            /// Create the runtime graph values that will be used for processing
            /// </summary>
            /// <param name="graph">The graph instance that will be used to process the results of the workflow</param>
            /// <param name="environmentVariables">The collection of environment variables that will be used</param>
            public RuntimeGraph(ProcessGraph graph,
                                Dictionary<string, string?> environmentVariables)
            {
                Graph = graph;
                EnvironmentVariables = environmentVariables;
            }
        }
    }
}
