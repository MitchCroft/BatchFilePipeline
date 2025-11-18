namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Define a graph that will be run once after the main processing graph
    /// </summary>
    internal sealed class PostProcessSupportGraph : SupportGraph
    {
        /*----------Variables----------*/
        //PRIVATE

        //TODO: Add variables that will be used by the post-process support graph for processing

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the graph with the required label
        /// </summary>
        public PostProcessSupportGraph() :
            base("Post-Process", Nodes.NodeUsage.PostProcess)
        {}

        /// <summary>
        /// Handle the setup required to execute the graph asynchronously
        /// </summary>
        /// <param name="environmentVariables">The collection of assigned environment variables for processing</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the execution of the process</param>
        /// <returns>Returns the status code of the operation once it has finished running</returns>
        public override ValueTask<int> EvaluateGraphAsync(Dictionary<string, string?> environmentVariables,
                                                          CancellationToken cancellationToken)
        {
            return ExecuteGraphAsync(environmentVariables, new Dictionary<string, object?>(), cancellationToken);
        }

        //PROTECTED

        /// <summary>
        /// Check to see that all of the starting information is defined for the operation to progress
        /// </summary>
        /// <param name="environmentVariables">The collection of environment variables that have been defined for use</param>
        /// <returns>Returns true if the required information is present and available for processing</returns>
        protected override bool IdentifyStartingValues(Dictionary<string, string?> environmentVariables)
        {
            // TODO: Check that the values are good to go
            return true;
        }
    }
}
