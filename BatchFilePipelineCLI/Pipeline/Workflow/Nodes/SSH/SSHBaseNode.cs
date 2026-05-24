using BatchFilePipelineCLI.PropertyResolver;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH
{
    /// <summary>
    /// Defines a base class object that can be used to define the common elements of SSH related nodes
    /// </summary>
    internal abstract class SSHBaseNode
    {
        /*----------Variables-----------*/
        //PROTECTED

        /// <summary>
        /// There will need to be several pieces of common information that is required to connect to an SSH server
        /// </summary>
        protected readonly Property _hostProperty = Property.Create
        (
            "Host",
            "The address of the SSH server that should be connected to",
            typeof(string),
            "ssh.example.com or 192.168.0.1"
        );
        protected readonly Property _usernameProperty = Property.Create
        (
            "Username",
            "The username of the user account that will be used to authenticate to the SSH server",
            typeof(string)
        );
        protected readonly Property _passwordProperty = Property.Create
        (
            "Password",
            "The password of the user that will be used when authenticating via password authentication. This is only required if password authentication is being used, some form of authentication is required",
            string.Empty
        );
        protected readonly Property _privateKeyProperty = Property.Create
        (
            "PrivateKey",
            "The path to the private key file that will be used when authenticating via key based authentication. This is only required if key based authentication is being used, some form of authentication is required",
            string.Empty,
            "Path/To/PrivateKey/id_rsa"
        );
        protected readonly Property _privateKeyPassphraseProperty = Property.Create
        (
            "PrivateKeyPassphrase",
            "The passphrase that will be used to decrypt the private key file, if the private key is encrypted. This is only required if key based authentication is being used and the private key is encrypted",
            string.Empty
        );

        /*----------Functions-----------*/
        //PROTECTED

        /// <summary>
        /// Retrieve the formatted connection info that can be used to connect to the SSH server
        /// </summary>
        /// <param name="context">The context for the Node that is currently running</param>
        /// <returns>Returns the connection info object that can be used to process a remote connection</returns>
        protected ConnectionInfo GetConnectionInfo(PipelineExecutionContext context)
        {
            // Get the different pieces of information that will be used for processing the connection
            string host = context.GetInput<string>(_hostProperty);
            string username = context.GetInput<string>(_usernameProperty);
            string password = context.GetInput<string>(_passwordProperty);
            string privateKeyPath = context.GetInput<string>(_privateKeyProperty);
            string privateKeyPassphrase = context.GetInput<string>(_privateKeyPassphraseProperty);

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
            return new ConnectionInfo
            (
                host,
                username,
                authenticationMethods.ToArray()
            );
        }

        /// <summary>
        /// Combine the supplied collection of additional properties with the common SSH input properties
        /// </summary>
        /// <param name="additional">The collection of additional properties that are required for operation</param>
        /// <returns>Returns the collection of properties that can be used for processing</returns>
        protected IList<Property> CombineInputs(params Property[] additional)
        {
            Property[] combined = new Property[additional.Length + 5];
            combined[0] = _hostProperty;
            combined[1] = _usernameProperty;
            combined[2] = _passwordProperty;
            combined[3] = _privateKeyProperty;
            combined[4] = _privateKeyPassphraseProperty;
            Array.Copy(additional, 0, combined, 5, additional.Length);
            return combined;
        }
    }
}
