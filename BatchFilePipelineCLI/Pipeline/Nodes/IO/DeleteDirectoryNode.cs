using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Nodes.IO
{
    /// <summary>
    /// Handle the removal of a directory that is no longer needed during a workflow
    /// </summary>
    [Node]
    public sealed class DeleteDirectoryNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Define the properties that will be needed to operate the operation
        /// </summary>
        private readonly Property _targetDirProperty = Property.Create
        (
            "TargetDir",
            "The path to the directory that is to be removed",
            typeof(string),
            "Path/To/Directory/"
        );
        private readonly Property _recursiveProperty = Property.Create
        (
            "Recursive",
            "Flags if the delete operation should be recursive, cleaning out all sub-directories and files as well",
            true
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_targetDirProperty, _recursiveProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => Array.Empty<Property>();

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            // Get the elements that will need processing
            string targetDir = context.GetInput<string>(_targetDirProperty);
            bool recursive = context.GetInput<bool>(_recursiveProperty);

            // Copy the file over
            try
            {
                Directory.Delete
                (
                    targetDir,
                    recursive
                );
            } catch (DirectoryNotFoundException) { }
            return ValueTask.FromResult(new ExecutionResult());
        }
    }
}
