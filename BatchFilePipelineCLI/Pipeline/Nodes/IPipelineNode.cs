namespace BatchFilePipelineCLI.Pipeline.Nodes
{
    /// <summary>
    /// Defines the interface for a node element that can exist in a workflow and be used to perform an action
    /// </summary>
    public interface IPipelineNode
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<NodeProperty> GetInputProperties();

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<NodeProperty> GetOutputProperties();

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<NodeOutput> ProcessNodeResultAsync(IDictionary<string, object?> inputs);
    }
}
