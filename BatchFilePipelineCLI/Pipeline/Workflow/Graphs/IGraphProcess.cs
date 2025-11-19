using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Interface that can be used for all handlers that will be used to manage the executing of a graph of work
    /// </summary>
    internal interface IGraphProcess
    {
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
        public bool TryLoadFromDescription(GraphDescription description,
                                           NodeLibrary library,
                                           IDictionary<string, string?> environmentVariables,
                                           IDictionary<string, string?> argumentVariables);

        /// <summary>
        /// Handle the process of evaluating the defined graph with the specified values
        /// </summary>
        /// <param name="runtimeVariables">A collection of existing runtime variables that can be used for processing</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the result of the execution process</returns>
        public ValueTask<ExecutionResult> EvaluateGraphAsync(IDictionary<string, object?> runtimeVariables,
                                                             CancellationToken cancellationToken);
    }
}
