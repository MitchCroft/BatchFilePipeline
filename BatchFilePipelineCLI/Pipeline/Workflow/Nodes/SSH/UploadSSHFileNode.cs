using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.ExecutionState;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH
{
    /// <summary>
    /// Upload a file to a remote SSH server from a local path
    /// </summary>
    [PipelineNode(nameof(UploadSSHFileNode), NodeUsage.Process)]
    internal sealed class UploadSSHFileNode : SSHBaseNode, IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We only need a few pieces of information to be able to upload a file to the remote server
        /// </summary>
        private readonly Property _targetFileProperty = Property.Create
        (
            "TargetFile",
            "The path to the file on the local system that is to be uploaded to the remote SSH server",
            typeof(string),
            "Path/To/Local/File.txt"
        );
        private readonly Property _destinationPathProperty = Property.Create
        (
            "DestinationPath",
            "The path on the remote SSH server where the file should be uploaded to",
            typeof(string),
            "/remote/path/to/file.txt"
        );

        /// <summary>
        /// We can serve up the file again after it has been uploaded
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out the path for the remote file that was uploaded",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => CombineInputs(_targetFileProperty, _destinationPathProperty);

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
            string targetFile = (string)inputs[_targetFileProperty.Name]!;
            string destinationPath = ((string)inputs[_destinationPathProperty.Name]!).Replace('\\', '/');

            // Try to make the SSH connection
            using var sftp = new SftpClient(connectionInfo);
            sftp.Connect();

            // We need to ensure that the system doesn't sleep during file transfers
            using var marker = ExecutionStateHandler.Push();

            // Ensure that the target directory exists
            await sftp.CreateRemoteDirectoryAsync(Path.GetDirectoryName(destinationPath)!, cancellationToken);
            if (cancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }

            // Open the local file so that it can be read and uploaded
            using (var fStream = File.OpenRead(targetFile))
            {
                Logger.Log($"[{nameof(UploadSSHFileNode)}] Uploading file '{targetFile}' to remote '{destinationPath}'");
                await sftp.UploadFileAsync(fStream, destinationPath, cancellationToken);
            }
            if (cancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }
            Logger.Success($"[{nameof(UploadSSHFileNode)}] Uploaded file '{targetFile}' to remote '{destinationPath}'");

            // We have the results of the file upload
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, destinationPath }
                }
            );
        }
    }
}
