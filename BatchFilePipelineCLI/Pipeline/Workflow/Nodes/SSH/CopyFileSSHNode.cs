using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.ExecutionState;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH
{
    /// <summary>
    /// Defines a node that can be used to copy a file from one remote location to another
    /// </summary>
    [PipelineNode(nameof(CopyFileSSHNode), NodeUsage.Process)]
    internal sealed class CopyFileSSHNode : SSHBaseNode, IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need to know from where and to where the file should be copied
        /// </summary>
        private readonly Property _sourcePathProperty = Property.Create
        (
            "Source",
            "The path to the remote file that is to be copied",
            typeof(string),
            "/path/to/source/file.txt"
        );
        private readonly Property _destinationPathProperty = Property.Create
        (
            "Destination",
            "The path to the final location of the remote file that is to be processed",
            typeof(string),
            "/path/to/destination/file.txt"
        );
        private readonly Property _allowOverwriteProperty = Property.Create
        (
            "AllowOverwrite",
            "Flags if an existing file at the destination path should be overwritten",
            false
        );
        
        /// <summary>
        /// Define the properties that will be passed out by this Node once finished processing
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out the destination path of the copied file",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => CombineInputs(_sourcePathProperty, _destinationPathProperty, _allowOverwriteProperty);

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
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                       CancellationToken cancellationToken)
        {
            // Retrieve the properties that will be processed
            ConnectionInfo connectionInfo = GetConnectionInfo(context);
            string sourcePath = context.GetInput<string>(_sourcePathProperty);
            string destinationPath = context.GetInput<string>(_destinationPathProperty);
            bool allowOverwrite = context.GetInput<bool>(_allowOverwriteProperty);

            // Try to make the SSH connection
            using var sftp = new SftpClient(connectionInfo);
            sftp.Connect();

            // We need to ensure that the system doesn't sleep during file transfers
            using var marker = ExecutionStateHandler.Push();

            // Ensure that the destination directory exists
            await sftp.CreateRemoteDirectoryAsync(Path.GetDirectoryName(destinationPath)!, cancellationToken);

            // Copy the data from the source file to the destination
            Logger.Log($"[{nameof(CopyFileSSHNode)}] Copying remote file '{sourcePath}' to '{destinationPath}'");
            await sftp.CopyRemoteFileAsync(sourcePath, destinationPath, cancellationToken, allowOverwrite);
            if (cancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }
            Logger.Success($"[{nameof(CopyFileSSHNode)}] Successfully copied remote file '{sourcePath}' to '{destinationPath}'");

            // We have the results of the file copy operation
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
