using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Text;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.Email
{
    /// <summary>
    /// Provide a Node that can be used to interface with an external mail server and send a message
    /// </summary>
    [Node]
    public sealed class SendEmailNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Define the collection of properties that will be needed to send an email via the SMTP client
        /// </summary>
        private readonly Property _fromProperty = Property.Create
        (
            "From",
            "The email address that the email being sent should say it is from",
            typeof(string),
            "example@domain.com"
        );
        private readonly Property _fromNameProperty = Property.Create
        (
            "FromName",
            "The display name for the address that the email being sent should say it is from",
            typeof(string),
            "My Pipeline"
        );
        private readonly Property _toProperty = Property.Create
        (
            "To",
            "The email address that the email should be sent to",
            typeof(string),
            "example@domain.com"
        );
        private readonly Property _toNameProperty = Property.Create
        (
            "ToName",
            "The display name for the address that the email being sent to",
            "Recipient"
        );
        private readonly Property _subjectProperty = Property.Create
        (
            "Subject",
            "The subject line that will be assigned to the email being sent",
            typeof(string)
        );
        private readonly Property _bodyProperty = Property.Create
        (
            "Body",
            "The body of the text that should be sent alongside the message",
            typeof(string)
        );
        private readonly Property _hostProperty = Property.Create
        (
            "Host",
            "The address of the outgoing mail server that can be used to send the message",
            typeof(string),
            "smtp.gmail.com"
        );
        private readonly Property _portProperty = Property.Create
        (
            "Port",
            "The port on the outgoing mail server that should be used to send the message",
            typeof(int),
            "587"
        );
        private readonly Property _usernameProperty = Property.Create
        (
            "Username",
            "The username of the account that will be used to authenticate the sending of the outgoing message",
            typeof(string)
        );
        private readonly Property _passwordProperty = Property.Create
        (
            "Password",
            "The password associated with the username for processing the authentication",
            typeof(string)
        );
        private readonly Property _secureSocketOptionsProperty = Property.Create
        (
            "SocketOptions",
            "The type of socket connection that should be used when processing the request",
            SecureSocketOptions.StartTls
        );
        private readonly Property _messageBodyTypeProperty = Property.Create
        (
            "BodyType",
            "The type of text that is being sent as the body of the message",
            TextFormat.Text
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [ _fromProperty, _fromNameProperty, _toProperty, _toNameProperty, _subjectProperty, _bodyProperty, _hostProperty, _portProperty, _usernameProperty, _passwordProperty, _secureSocketOptionsProperty, _messageBodyTypeProperty ];

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
            // Retrieve the values that are needed for proecssing
            string from = context.GetInput<string>(_fromProperty);
            string fromName = context.GetInput<string>(_fromNameProperty);
            string to = context.GetInput<string>(_toProperty);
            string toName = context.GetInput<string>(_toNameProperty);
            string subject = context.GetInput<string>(_subjectProperty);
            string body = context.GetInput<string>(_bodyProperty);
            string host = context.GetInput<string>(_hostProperty);
            int port = context.GetInput<int>(_portProperty);
            string username = context.GetInput<string>(_usernameProperty);
            string password = context.GetInput<string>(_passwordProperty);
            SecureSocketOptions socketOption = context.GetInput<SecureSocketOptions>(_secureSocketOptionsProperty);
            TextFormat format = context.GetInput<TextFormat>(_messageBodyTypeProperty);

            // Setup the message that is going to be sent
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(new MailboxAddress(toName, to));
            message.Subject = subject;
            var bodyContent = new TextPart(format);
            bodyContent.SetText(Encoding.UTF8, body);
            message.Body = bodyContent;

            // Create the client that will send the message
            using var client = new SmtpClient();

            // Create the connection to the client for processing
            await client.ConnectAsync(host, port, socketOption, context.CancellationToken);
            if (context.CancellationToken.IsCancellationRequested)
            {
                return default;
            }

            // Authenticate the connection so that we can send the message
            await client.AuthenticateAsync(username, password, context.CancellationToken);
            if (context.CancellationToken.IsCancellationRequested)
            {
                return default;
            }

            // Send the message to the receiver
            await client.SendAsync(message, context.CancellationToken);

            // Make sure the client is disconnected when done
            await client.DisconnectAsync(true, context.CancellationToken);

            return default;
        }
    }
}
