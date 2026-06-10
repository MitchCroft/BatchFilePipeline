using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility;
using BatchFilePipelineCLI.Utility.ExecutionState;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.SSH.Operations
{
    /// <summary>
    /// Delete a file from a remote SSH server
    /// </summary>
    [Node]
    public sealed class DeleteSSHFileNode : INode
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

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_clientProperty, _targetFileProperty];

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
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            // Retrieve the properties that will be processed
            SftpClient client = context.GetInput<SftpClient>(_clientProperty);
            string targetFile = IOUtility.CleanFilePath(context.GetInput<string>(_targetFileProperty));

            // We need to ensure that the system doesn't sleep during file transfers
            using var marker = ExecutionStateHandler.Push();

            // Remove the file from the remote server
            Logger.Log($"[{nameof(DeleteSSHFileNode)}] Deleting the file '{targetFile}'...");
            await client.DeleteFileAsync(targetFile, context.CancellationToken);

            // We have finished processing the file
            return new ExecutionResult
            (
                new Dictionary<string, object?>()
            );
        }
    }
}
