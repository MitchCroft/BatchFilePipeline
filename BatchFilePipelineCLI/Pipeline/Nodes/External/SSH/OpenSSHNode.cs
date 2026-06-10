using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Disposal;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.SSH
{
    /// <summary>
    /// Allow for the opening of an SSH connection, generating a client that can be used for actioning requests
    /// </summary>
    [Node]
    public sealed class OpenSSHNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There will need to be several pieces of common information that is required to connect to an SSH server
        /// </summary>
        private readonly Property _hostProperty = Property.Create
        (
            "Host",
            "The address of the SSH server that should be connected to",
            typeof(string),
            "ssh.example.com or 192.168.0.1"
        );
        private readonly Property _usernameProperty = Property.Create
        (
            "Username",
            "The username of the user account that will be used to authenticate to the SSH server",
            typeof(string)
        );
        private readonly Property _passwordProperty = Property.Create
        (
            "Password",
            "The password of the user that will be used when authenticating via password authentication. This is only required if password authentication is being used, some form of authentication is required",
            string.Empty
        );
        private readonly Property _privateKeyProperty = Property.Create
        (
            "PrivateKey",
            "The path to the private key file that will be used when authenticating via key based authentication. This is only required if key based authentication is being used, some form of authentication is required",
            string.Empty,
            "Path/To/PrivateKey/id_rsa"
        );
        private readonly Property _privateKeyPassphraseProperty = Property.Create
        (
            "PrivateKeyPassphrase",
            "The passphrase that will be used to decrypt the private key file, if the private key is encrypted. This is only required if key based authentication is being used and the private key is encrypted",
            string.Empty
        );
        private readonly Property _connectionTimeoutProperty = Property.Create
        (
            "ConnectionTimeout",
            "The length of time (in seconds) that the connection will be kept open before the client will be closed. -1 will prevent the connection being closed",
            30
        );

        /// <summary>
        /// This node is going to create an SSH client that can be used in other Nodes for processing
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
        public IList<Property> GetInputProperties() => [_hostProperty, _usernameProperty, _passwordProperty, _privateKeyProperty, _privateKeyPassphraseProperty, _connectionTimeoutProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [ _clientProperty ];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            // Get the connection info that will be used to establish the SSH connection
            var connectionInfo = GetConnectionInfo(context);
            var sftpClient = new SftpClient(connectionInfo);
            DisposalBackup.Register(sftpClient);

            // Open the connection to the remote server
            await sftpClient.ConnectAsync(context.CancellationToken);

            // Return the client for use
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _clientProperty.Name, sftpClient }
                }
            );
        }

        //PRIVATE

        /// <summary>
        /// Retrieve the formatted connection info that can be used to connect to the SSH server
        /// </summary>
        /// <param name="context">The context for the Node that is currently running</param>
        /// <returns>Returns the connection info object that can be used to process a remote connection</returns>
        private ConnectionInfo GetConnectionInfo(PipelineContext context)
        {
            // Get the different pieces of information that will be used for processing the connection
            string host = context.GetInput<string>(_hostProperty);
            string username = context.GetInput<string>(_usernameProperty);
            string password = context.GetInput<string>(_passwordProperty);
            string privateKeyPath = context.GetInput<string>(_privateKeyProperty);
            string privateKeyPassphrase = context.GetInput<string>(_privateKeyPassphraseProperty);
            TimeSpan connectionTimeout = TimeSpan.FromSeconds(context.GetInput<int>(_connectionTimeoutProperty));

            // Determine the authentication methods that will be used for connecting to the server
            List<AuthenticationMethod> authenticationMethods = new(2);
            if (string.IsNullOrWhiteSpace(privateKeyPath) == false)
            {
                var keyFile = string.IsNullOrWhiteSpace(privateKeyPassphrase) == false ?
                    new PrivateKeyFile(privateKeyPath, privateKeyPassphrase) :
                    new PrivateKeyFile(privateKeyPath);
                authenticationMethods.Add(new PrivateKeyAuthenticationMethod(username, keyFile));
            }
            if (string.IsNullOrWhiteSpace(password) == false)
            {
                authenticationMethods.Add(new PasswordAuthenticationMethod(username, password));
            }

            // We have the result for making the connection
            var connectionInfo = new ConnectionInfo
            (
                host,
                username,
                authenticationMethods.ToArray()
            );
            connectionInfo.Timeout = connectionTimeout;
            return connectionInfo;
        }
    }
}
