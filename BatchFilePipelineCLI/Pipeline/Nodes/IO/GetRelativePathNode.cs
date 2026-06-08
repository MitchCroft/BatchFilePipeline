using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility;

namespace BatchFilePipelineCLI.Pipeline.Nodes.IO
{
    /// <summary>
    /// Define a node that can be used to calculate the relative path to another
    /// </summary>
    [Node]
    public sealed class GetRelativePathNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need the different input paths to be able to work out the relative path values
        /// </summary>
        private readonly Property _relativeToProperty = Property.Create
        (
            "RelativeTo",
            "The source path the result should be relative to. This path is always considered to be a directory",
            typeof(string),
            "Directory/"
        );
        private readonly Property _pathProperty = Property.Create
        (
            "Path",
            "The destination path",
            typeof(string),
            "Directory/SubDirectory/File.tmp"
        );

        /// <summary>
        /// This will result in a single value that describes the relative path of the file
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The relative path, or path if the paths don't share the same root",
            typeof(string),
            "SubDirectory/File.tmp"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [ _relativeToProperty, _pathProperty ];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [ _outputProperty ];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            string relativeTo = context.GetInput<string>(_relativeToProperty);
            string path = context.GetInput<string>(_pathProperty);
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, IOUtility.GetRelativePath(relativeTo, path) }
                }
            ));
        }
    }
}
