using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility;

namespace BatchFilePipelineCLI.Pipeline.Nodes.IO
{
    /// <summary>
    /// Define a Node that can be used to get the file extension from a specified file path
    /// </summary>
    [Node]
    public sealed class GetFileExtensionNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need to get the path of the file that is to be processed
        /// </summary>
        private readonly Property _pathProperty = Property.Create
        (
            "FilePath",
            "The full path to the file from which the extension is to be extracted",
            typeof(string),
            example: "Path/To/File/Example.txt"
        );

        /// <summary>
        /// Defines the property that will be used as an output of the node for use in later stages
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The string value that contains extension of the file path",
            typeof(string),
            example: ".txt"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_pathProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            string filePath = context.GetInput<string>(_pathProperty);
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, IOUtility.GetExtension(filePath) }
                }
            ));
        }
    }
}
