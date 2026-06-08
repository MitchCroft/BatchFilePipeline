using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.IO
{
    /// <summary>
    /// A Node that can be used to retrieve the file name from a specified path
    /// </summary>
    [PipelineNode(nameof(GetFileNameNode), NodeUsage.All)]
    internal sealed class GetFileNameNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need to get the path of the file that is to be processed
        /// </summary>
        private readonly Property _pathProperty = Property.Create
        (
            "FilePath",
            "The full path to the file from which the file name is to be extracted",
            typeof(string),
            example: "Path/To/File/Example.txt"
        );
        private readonly Property _includeExtension = Property.Create
        (
            "IncludeExtension",
            "Flags if the file extension should be included in the resulting file name",
            defaultValue: true
        );

        /// <summary>
        /// Defines the property that will be used as an output of the node for use in later stages
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The string value that contains the isolated name of the path",
            typeof(string),
            example: "Example.txt"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_pathProperty, _includeExtension];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                 CancellationToken cancellationToken)
        {
            string filePath = context.GetInput<string>(_pathProperty);
            bool includeExtension = context.GetInput<bool>(_includeExtension);
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, includeExtension ? Path.GetFileName(filePath) : Path.GetFileNameWithoutExtension(filePath) }
                }
            ));
        }
    }
}
