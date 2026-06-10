using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Disposal;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.SSH
{
    /// <summary>
    /// Close an active <see cref="SftpClient"/> objects connection and clean up its values
    /// </summary>
    [Node]
    public sealed class CloseSSHNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// This node is just used to shutdown a client connection that was created previously
        /// </summary>
        private readonly Property _clientProperty = Property.Create
        (
            "Client",
            "An instance of the SSHClient that can be supplied to SSH nodes to action requests",
            typeof(SftpClient)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_clientProperty];

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
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            SftpClient client = context.GetInput<SftpClient>(_clientProperty);
            DisposalBackup.Unregister(client);
            return ValueTask.FromResult(new ExecutionResult());
        }
    }
}
