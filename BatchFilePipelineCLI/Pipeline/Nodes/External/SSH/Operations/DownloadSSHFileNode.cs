using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility;
using BatchFilePipelineCLI.Utility.ExecutionState;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.SSH.Operations
{
    /// <summary>
    /// Download a file from a remote SSH server to a local path
    /// </summary>
    [Node]
    public sealed class DownloadSSHFileNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We only need a few pieces of information to be able to download a file from the remote server
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
            "The path to the file on the remote SSH server that should be downloaded",
            typeof(string),
            "/remote/path/to/file.txt"
        );
        private readonly Property _destinationPathProperty = Property.Create
        (
            "DestinationPath",
            "The local path where the downloaded file should be saved",
            typeof(string),
            "Path/To/File.txt"
        );

        /// <summary>
        /// We can serve up the file again after it has been downloaded
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out the path for the local file that was downloaded",
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
            string targetFile = context.GetInput<string>(_targetFileProperty);
            string destinationPath = context.GetInput<string>(_destinationPathProperty);

            // We need to ensure that the system doesn't sleep during file transfers
            using var marker = ExecutionStateHandler.Push();

            // Download the file from the remote server to disc for processing
            Logger.Log($"[{nameof(DownloadSSHFileNode)}] Creating directory for '{destinationPath}'");
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            using (var fStream = File.Create(IOUtility.CleanFilePath(destinationPath)))
            {
                Logger.Log($"[{nameof(DownloadSSHFileNode)}] Downloading remote file '{targetFile}' to '{destinationPath}'");
                await client.DownloadFileAsync(IOUtility.CleanFilePath(targetFile), fStream, context.CancellationToken);
            }
            if (context.CancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }
            Logger.Success($"[{nameof(DownloadSSHFileNode)}] Downloaded remote file '{targetFile}' to '{destinationPath}'");

            // We have the results of the file download
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
