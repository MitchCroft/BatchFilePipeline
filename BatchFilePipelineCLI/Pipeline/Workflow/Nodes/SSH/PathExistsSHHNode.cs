using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;
using Renci.SshNet;
using System.Globalization;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH
{
    /// <summary>
    /// Define a Node that can be used to check if a path exists at a remote location
    /// </summary>
    [PipelineNode(nameof(PathExistsSHHNode), NodeUsage.All)]
    internal sealed class PathExistsSHHNode : SSHBaseNode, IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need to get some different pieces of information to know what to check
        /// </summary>
        private readonly Property _pathProperty = Property.Create
        (
            "Path",
            "The path that should be checked for existence on the remote SSH server",
            typeof(string),
            "Parent/Directory/"
        );

        /// <summary>
        /// This node can just pass out a simple boolean value indicating whether or not the directory exists at the specified path on the remote SSH server
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The resulting boolean value indicating whether or not the path exists at the specified path on the remote SSH server",
            typeof(bool)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => CombineInputs(_pathProperty);

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
            string path = context.GetInput<string>(_pathProperty);

            // Try to make the SSH connection
            using var sftp = new SftpClient(connectionInfo);
            sftp.Connect();

            // Check to see if the path exists on the remote server
            bool exists = await sftp.ExistsAsync(path.Replace('\\', '/'), cancellationToken);
            if (cancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }

            // We have the results of the check operation
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, exists }
                },
                nextNode: exists.ToString(CultureInfo.InvariantCulture)
            );
        }
    }
}
