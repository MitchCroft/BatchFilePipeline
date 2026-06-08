using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.ExecutionState;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH
{
    /// <summary>
    /// Delete a file from a remote SSH server
    /// </summary>
    [PipelineNode(nameof(DeleteSSHFileNode), NodeUsage.Process)]
    internal sealed class DeleteSSHFileNode : SSHBaseNode, INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We only need a few pieces of information to be able to download a file from the remote server
        /// </summary>
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
        public IList<Property> GetInputProperties() => CombineInputs(_targetFileProperty);

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => Array.Empty<Property>();

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                       CancellationToken cancellationToken)
        {
            // Retrieve the properties that will be processed
            ConnectionInfo connectionInfo = GetConnectionInfo(context);
            string targetFile = context.GetInput<string>(_targetFileProperty);

            // Try to make the SSH connection
            using var sftp = new SftpClient(connectionInfo);
            sftp.Connect();

            // We need to ensure that the system doesn't sleep during file transfers
            using var marker = ExecutionStateHandler.Push();

            // Remove the file from the remote server
            Logger.Log($"[{nameof(DeleteSSHFileNode)}] Deleting the file '{targetFile}'...");
            await sftp.DeleteFileAsync(targetFile, cancellationToken);

            // We have finished processing the file
            return new ExecutionResult
            (
                new Dictionary<string, object?>()
            );
        }
    }
}
