using BatchFilePipelineCLI.DynamicProperties;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Utility
{
    /// <summary>
    /// A special case node that can be used to mark values values that will be exported from the graph into the next
    /// </summary>
    /// <remarks>
    /// This node is a special use case on the graph that can be used to export values from the node working flow
    /// to the next graphs in the sequence for processing. Values that are defined as inputs will be exported to
    /// be used in later stages of processing
    /// </remarks>
    [PipelineNode(nameof(ExportValuesNode), NodeUsage.All)]
    internal sealed class ExportValuesNode : IPipelineNode
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => throw new NotImplementedException($"[{nameof(ExportValuesNode)}] Special case node, inputs should be description defined");

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => throw new NotImplementedException($"[{nameof(ExportValuesNode)}] Special case node, outputs should be description defined");

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(IDictionary<string, object?> inputs,
                                                                 CancellationToken cancellationToken) => throw new NotImplementedException($"[{nameof(ExportValuesNode)}] Special case node, processing should be handled separetly");
    }
}
