using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.IO
{
    /// <summary>
    /// Copy a directory from one location to another
    /// </summary>
    [PipelineNode(nameof(CopyDirectoryNode), NodeUsage.All)]
    internal sealed class CopyDirectoryNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There are some properties that we can use while processing the data
        /// </summary>
        private readonly Property _targetDirProperty = Property.Create
        (
            "TargetDir",
            "The path to the directory that is to be copied, with all of it's contents",
            typeof(string),
            "Path/To/Directory"
        );
        private readonly Property _destinationPathProperty = Property.Create
        (
            "DestinationPath",
            "The path to the parent location where the directory should be placed",
            typeof(string),
            "New/Path/To"
        );
        private readonly Property _renameDirectoryProperty = Property.Create
        (
            "RenameDirectory",
            "An optional name that can be used to rename the directory that is being copied. If left empty, will use the existing name",
            string.Empty
        );
        private readonly Property _allowOverwriteProperty = Property.Create
        (
            "AllowOverwrite",
            "Flags if files are allowed to be overwriten during the copy operation",
            false
        );

        /// <summary>
        /// We can output the name of the directory when we are done
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Outputs the final path to the root of the directory that was copied",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_targetDirProperty, _destinationPathProperty, _renameDirectoryProperty, _allowOverwriteProperty];

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
            // Get the values that we need
            string targetDir = context.GetInput<string>(_targetDirProperty);
            string destinationPath = context.GetInput<string>(_destinationPathProperty);
            string renameDirectory = context.GetInput<string>(_renameDirectoryProperty);
            bool allowOverwrite = context.GetInput<bool>(_allowOverwriteProperty);

            // Get the root directory that is to be copied
            var rootInfo = new DirectoryInfo(targetDir);
            if (rootInfo.Exists == false)
            {
                throw new NullReferenceException($"[{nameof(CopyDirectoryNode)}] Unable to copy directory '{targetDir}', no such directory exists");
            }

            // Get the root of the destination path
            string destinationRoot = Path.Combine(destinationPath, string.IsNullOrWhiteSpace(renameDirectory) == true ? rootInfo.Name : renameDirectory);

            // We're going to need to process all of the files and sub-directories that are contained in the directory
            Queue<(DirectoryInfo source, string destination)> toCopy = new();
            toCopy.Enqueue((rootInfo, destinationRoot));

            // Process the copying of the different elements to the target location
            while (toCopy.TryDequeue(out var current))
            {
                // Make sure that the destination location exists
                Logger.Log($"[{nameof(CopyDirectoryNode)}] Creating directory '{current.destination}'");
                Directory.CreateDirectory(current.destination);

                // Copy all of the files in the source
                foreach (var file in current.source.EnumerateFiles())
                {
                    Logger.Log($"[{nameof(CopyDirectoryNode)}] Copying file '{file.FullName}'");
                    string finalPath = Path.Combine(current.destination, file.Name);
                    File.Copy(file.FullName, finalPath, allowOverwrite);
                }

                // Queue up the contained sub-directories
                foreach (var subDir in current.source.EnumerateDirectories())
                {
                    toCopy.Enqueue((subDir, Path.Combine(current.destination, subDir.Name)));
                }
            }

            // We have our final directory copied
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, destinationRoot }
                }
            ));
        }
    }
}
