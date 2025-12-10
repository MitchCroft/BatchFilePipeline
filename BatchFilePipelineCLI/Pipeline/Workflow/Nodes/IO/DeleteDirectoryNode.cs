using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.IO
{
    /// <summary>
    /// Handle the removal of a directory that is no longer needed during a workflow
    /// </summary>
    [PipelineNode(nameof(DeleteDirectoryNode), NodeUsage.All)]
    internal sealed class DeleteDirectoryNode : IPipelineNode
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
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(IReadOnlyDictionary<string, object?> inputs,
                                                                 CancellationToken cancellationToken)
        {
            // Process the copy operation
            try
            {
                // Get the elements that will need processing
                string targetDir = (string)inputs[_targetDirProperty.Name]!;
                bool recursive = (bool)inputs[_recursiveProperty.Name]!;

                // Copy the file over
                try
                {
                    Directory.Delete
                    (
                        targetDir,
                        recursive
                    );
                }
                catch (DriveNotFoundException) {}
                return ValueTask.FromResult(new ExecutionResult
                (
                    new Dictionary<string, object?>()
                ));
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return ValueTask.FromResult(new ExecutionResult(ex)); }
        }
    }
}
