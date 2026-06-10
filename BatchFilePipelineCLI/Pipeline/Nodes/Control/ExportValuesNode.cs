using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Control
{
    /// <summary>
    /// Special case node that can be used to define values that need to be passed up an execution level for processing in
    /// later sections
    /// </summary>
    [Node]
    public sealed class ExportValuesNode : INode
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => throw new NotImplementedException($"[{nameof(ExportValuesNode)}] Special case node, regular {nameof(INode)} functions shouldn't be called");

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => throw new NotImplementedException($"[{nameof(ExportValuesNode)}] Special case node, regular {nameof(INode)} functions shouldn't be called");

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context) => throw new NotImplementedException($"[{nameof(ExportValuesNode)}] Special case node, regular {nameof(INode)} functions shouldn't be called");
    }
}
