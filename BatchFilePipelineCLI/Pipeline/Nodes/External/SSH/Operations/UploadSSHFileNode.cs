using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Nodes.External.SSH.Utility;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility;
using BatchFilePipelineCLI.Utility.ExecutionState;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.SSH.Operations
{
    /// <summary>
    /// Upload a file to a remote SSH server from a local path
    /// </summary>
    [Node]
    public sealed class UploadSSHFileNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We only need a few pieces of information to be able to upload a file to the remote server
        /// </summary>
        private readonly Property _clientProperty = Property.Create
        (
            "Client",
            "An instance of the SSHClient that can be supplied to SSH nodes to action requests",
            typeof(SftpClient)
        );
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
        public IList<Property> GetInputProperties() => [_clientProperty, _targetFileProperty, _destinationPathProperty];

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
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            // Retrieve the properties that will be processed
            SftpClient client = context.GetInput<SftpClient>(_clientProperty);
            string targetFile = IOUtility.CleanFilePath(context.GetInput<string>(_targetFileProperty));
            string destinationPath = IOUtility.CleanFilePath(context.GetInput<string>(_destinationPathProperty));

            // We need to ensure that the system doesn't sleep during file transfers
            using var marker = ExecutionStateHandler.Push();

            // Ensure that the target directory exists
            await client.CreateRemoteDirectoryAsync(Path.GetDirectoryName(destinationPath)!, context.CancellationToken);
            if (context.CancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }

            // Open the local file so that it can be read and uploaded
            using (var fStream = File.OpenRead(targetFile))
            {
                Logger.Log($"[{nameof(UploadSSHFileNode)}] Uploading file '{targetFile}' to remote '{destinationPath}'");
                await client.UploadFileAsync(fStream, destinationPath, context.CancellationToken);
            }
            if (context.CancellationToken.IsCancellationRequested == true)
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
