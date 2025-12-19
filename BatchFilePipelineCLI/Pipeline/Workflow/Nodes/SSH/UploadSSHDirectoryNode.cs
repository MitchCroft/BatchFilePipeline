using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.IO;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.ExecutionState;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH
{
    /// <summary>
    /// Upload all files contained within a directory to a remote SSH server from a local path
    /// </summary>
    [PipelineNode(nameof(UploadSSHDirectoryNode), NodeUsage.Process)]
    internal sealed class UploadSSHDirectoryNode : SSHBaseNode, IPipelineNode
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
        private readonly Property _failOnDuplicateProperty = Property.Create
        (
            "FailOnDuplicate",
            "Flags if, when overwrite is not allowed, should the operation return a failure if a duplicate is found",
            true
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
        public IList<Property> GetInputProperties() => CombineInputs(_targetDirProperty, _destinationPathProperty, _renameDirectoryProperty, _allowOverwriteProperty, _failOnDuplicateProperty);

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(IReadOnlyDictionary<string, object?> inputs,
                                                                       CancellationToken cancellationToken)
        {
            // Retrieve the properties that will be processed
            ConnectionInfo connectionInfo = GetConnectionInfo(inputs);
            string targetDir = (string)inputs[_targetDirProperty.Name]!;
            string destinationPath = (string)inputs[_destinationPathProperty.Name]!;
            string renameDirectory = (string)inputs[_renameDirectoryProperty.Name]!;
            bool allowOverwrite = (bool)inputs[_allowOverwriteProperty.Name]!;
            bool failOnDuplicate = (bool)inputs[_failOnDuplicateProperty.Name]!;

            // Get the root directory that is to be copied
            var rootInfo = new DirectoryInfo(targetDir);
            if (rootInfo.Exists == false)
            {
                throw new NullReferenceException($"[{nameof(CopyDirectoryNode)}] Unable to copy directory '{targetDir}', no such directory exists");
            }

            // Get the root of the destination path
            string destinationRoot = Path.Combine(destinationPath, string.IsNullOrWhiteSpace(renameDirectory) == true ? rootInfo.Name : renameDirectory)
                .Replace('\\', '/');

            // Try to make the SSH connection
            using var sftp = new SftpClient(connectionInfo);
            sftp.Connect();

            // We need to ensure that the system doesn't sleep during file transfers
            using var marker = ExecutionStateHandler.Push();

            // We're going to need to process all of the files and sub-directories that are contained in the directory
            Queue<(DirectoryInfo source, string destination)> toCopy = new();
            toCopy.Enqueue((rootInfo, destinationRoot));

            // Process the copying of the different elements to the target location
            while (toCopy.TryDequeue(out var current))
            {
                // Make sure that the destination location exists
                Logger.Log($"[{nameof(UploadSSHDirectoryNode)}] Creating directory '{current.destination}'");
                await sftp.CreateRemoteDirectoryAsync(current.destination, cancellationToken);
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return new ExecutionResult();
                }

                // Copy all of the files in the source
                foreach (var file in current.source.EnumerateFiles())
                {
                    // Work out where we're sending the file
                    string finalPath = Path.Combine(current.destination, file.Name).Replace('\\', '/');

                    // Check to see if the file already exists and if we can overwrite it
                    if (allowOverwrite == false &&
                        await sftp.ExistsAsync(finalPath, cancellationToken) == true)
                    {
                        if (failOnDuplicate == true)
                        {
                            return new ExecutionResult(409, $"[{nameof(UploadSSHDirectoryNode)}] Unable to upload file '{file.FullName}' to '{finalPath}' as a file already exists");
                        }
                        Logger.Log($"[{nameof(UploadSSHDirectoryNode)}] Skipping file '{file.FullName}' as it already exists on the server at '{finalPath}'");
                        continue;
                    }

                    // Process the file upload
                    Logger.Log($"[{nameof(UploadSSHDirectoryNode)}] Uploading file '{file.FullName}' to '{finalPath}'");
                    using (var fStream = File.OpenRead(file.FullName))
                    {
                        await sftp.UploadFileAsync(fStream, finalPath, cancellationToken);
                    }
                    if (cancellationToken.IsCancellationRequested == true)
                    {
                        return new ExecutionResult();
                    }
                    Logger.Log($"[{nameof(UploadSSHDirectoryNode)}] Uploaded file '{file.FullName}' to '{finalPath}'");
                }

                // Queue up the contained sub-directories
                foreach (var subDir in current.source.EnumerateDirectories())
                {
                    toCopy.Enqueue((subDir, Path.Combine(current.destination, subDir.Name).Replace('\\', '/')));
                }
            }

            // We're done copying the directory
            Logger.Success($"[{nameof(UploadSSHDirectoryNode)}] Finished uploading '{rootInfo.FullName}' to '{destinationRoot}'");

            // We have our final directory copied
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, destinationRoot }
                }
            );
        }
    }
}
