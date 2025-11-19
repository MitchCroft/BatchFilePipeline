using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Handle the running of a generic graph description without any additional work
    /// </summary>
    internal sealed class GenericGraphProcess : IGraphProcess
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Flags the type of nodes that are valid for use on this graph
        /// </summary>
        private readonly NodeUsage _validNodes;

        /// <summary>
        /// The runner that will be used for to execute the graph description
        /// </summary>
        private GraphRunner? _graphRunner;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Define the types of nodes that are able to be used on this graph
        /// </summary>
        /// <param name="validNodes">A mask of the type of nodes that can be used on this graph</param>
        public GenericGraphProcess(NodeUsage validNodes)
        {
            _validNodes = validNodes;
        }

        /// <summary>
        /// Attempt to load the graph object with the given description of nodes that need to be processed
        /// </summary>
        /// <param name="description">The description of the graph that is to be processed</param>
        /// <param name="library">The library of nodes that are available for use</param>
        /// <param name="environmentVariables">The collection of environment variables that are available for use</param>
        /// <param name="argumentVariables">The collection of command line argument variables that have been supplied to the program for use</param>
        /// <returns>Returns true if the graph process could be loaded properly for use</returns>
        public bool TryLoadFromDescription(GraphDescription description,
                                           NodeLibrary library,
                                           IDictionary<string, string?> environmentVariables,
                                           IDictionary<string, string?> argumentVariables)
        {
            return GraphRunnerBuilder.TryBuildGraphRunner(description, library, _validNodes, environmentVariables, argumentVariables, out _graphRunner);
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
            // Check that we have a graph to run
            if (_graphRunner == null)
            {
                return new ExecutionResult(new NullReferenceException($"Invalid graph runner assigned for processing"));
            }

            // Try to execute the data on the graph
            return await _graphRunner.ExecuteGraphAsync(runtimeVariables, cancellationToken);
        }

        /// <summary>
        /// Format a string representation of the process that is being handled
        /// </summary>
        public override string ToString() => $"{nameof(GenericGraphProcess)}.{_validNodes}";
    }
}
