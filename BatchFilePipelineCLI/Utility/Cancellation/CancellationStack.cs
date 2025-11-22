using BatchFilePipelineCLI.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Utility.Cancellation
{
    /// <summary>
    /// Manage a stack of cancellation tokens that can be used for controlled cancellation at varying levels of execution
    /// </summary>
    internal static class CancellationStack
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The root cancellation token that will be used to control all generated ones
        /// </summary>
        private static readonly CancellationTokenSource _rootToken = new();

        /// <summary>
        /// Defines a stack of the cancellation tokens that are actively being used for management
        /// </summary>
        private static readonly Stack<CancellationTokenSource> _tokenStack = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Register the callbacks required to process the receiving of cancel signals
        /// </summary>
        public static void Register()
        {
            Console.CancelKeyPress -= ReceiveCancelRequest;
            Console.CancelKeyPress += ReceiveCancelRequest;
        }

        /// <summary>
        /// Create a new token on the stack and store it for later use
        /// </summary>
        /// <param name="additional">The collection of additional tokens that should be wrapped in controlling the state</param>
        /// <returns>Returns the disposable cancellation token that can be used for reference</returns>
        public static DisposableCancellationToken PushSource(params CancellationToken[] additional)
        {
            var prior = _tokenStack.Count > 0 ? _tokenStack.Peek() : _rootToken;
            var tokens = new[] { prior.Token };
            if (additional?.Length > 0)
            {
                tokens = tokens.Concat(additional).ToArray();
            }
            var next = CancellationTokenSource.CreateLinkedTokenSource(tokens);
            _tokenStack.Push(next);
            return new DisposableCancellationToken(next.Token);
        }

        /// <summary>
        /// Attempt to pop the next token from the stack
        /// </summary>
        /// <param name="token">The token that is expected to be removed the stack</param>
        /// <returns>Returns true if the token could be removed</returns>
        public static bool PopSource(CancellationToken token)
        {
            if (_tokenStack.Count == 0)
            {
                throw new ArgumentException($"[{nameof(CancellationStack)}] Unable to pop next token, stack is empty");
            }
            if (_tokenStack.Peek().Token.Equals(token) == false)
            {
                Logger.Warning($"[{nameof(CancellationStack)}] The supplied token is not next on the stack");
                return false;
            }
            var next = _tokenStack.Pop();
            next.Cancel();
            return true;
        }

        /// <summary>
        /// Cancel all of the tokens that are contained in the collection
        /// </summary>
        public static void CancelAll() => _rootToken.Cancel();

        //PRIVATE

        /// <summary>
        /// Respond to a request to cancel from the user
        /// </summary>
        /// <param name="sender">The object that sent the cancel request</param>
        /// <param name="e">Information about the event that was created</param>
        private static void ReceiveCancelRequest(object? sender, ConsoleCancelEventArgs e)
        {
            // Check there are tokens that can be cancelled
            if (_tokenStack.Count == 0)
            {
                Logger.Error($"[{nameof(CancellationStack)}] Unable to process cancel operation, there are no tokens left in the stack");
                return;
            }

            // Cancel the last token on the stack
            var source = _tokenStack.Pop();
            source.Cancel();

            // We don't want the event to process anymore
            e.Cancel = true;
        }

        /*----------Types----------*/
        //PUBLIC

        /// <summary>
        /// Store a cancellation token that can be disposed of for use
        /// </summary>
        public struct DisposableCancellationToken : IDisposable, IEquatable<DisposableCancellationToken>, IEquatable<CancellationToken>
        {
            /*----------Variables----------*/
            //PUBLIC

            /// <summary>
            /// The token that is being used at this level
            /// </summary>
            public readonly CancellationToken Token;

            //PRIVATE

            /// <summary>
            /// Flags if this object has been disposed
            /// </summary>
            private bool _isDisposed;

            /*----------Properties----------*/
            //PUBLIC

            /// <summary>
            /// Flags if the token has been cancelled
            /// </summary>
            public bool IsCancellationRequested => Token.IsCancellationRequested;

            /*----------Functions----------*/
            //PUBLIC

            /// <summary>
            /// Create the container with the token at this level
            /// </summary>
            /// <param name="token">The token that is to be used for processing</param>
            public DisposableCancellationToken(CancellationToken token) => Token = token;

            /// <summary>
            /// Try to pop this token from the active stack
            /// </summary>
            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
                CancellationStack.PopSource(Token);
            }

            /// <summary>
            /// Check if this object equals another cancellation token
            /// </summary>
            /// <param name="other">The other token that it is to be compared against</param>
            /// <returns>Returns true if the two tokens are identical</returns>
            public bool Equals(CancellationToken other) => Token.Equals(other);

            /// <summary>
            /// Check if this object equals another cancellation token
            /// </summary>
            /// <param name="other">The other token that it is to be compared against</param>
            /// <returns>Returns true if the two tokens are identical</returns>
            bool IEquatable<DisposableCancellationToken>.Equals(DisposableCancellationToken other) => Token.Equals(other.Token);

            /// <summary>
            /// Check to see if this object is equal to another
            /// </summary>
            /// <param name="other">The other object to be compared against</param>
            /// <returns>Returns true if both objects refer to the same cancellation token</returns>
            public override bool Equals([NotNullWhen(true)] object? other)
            {
                if (other == null)
                {
                    return false;
                }
                switch (other)
                {
                    case CancellationToken token: return Token.Equals(token);
                    case DisposableCancellationToken disposableToken: return Token.Equals(disposableToken.Token);
                    default: return false;
                }
            }

            /// <summary>
            /// Retrieve the hash code for the contained cancellation token
            /// </summary>
            public override int GetHashCode() => Token.GetHashCode();

            /// <summary>
            /// Allow for implicit casting of the disposable source to the underlying token
            /// </summary>
            /// <param name="cancellationToken">The cancellation token source that is to be converted</param>
            public static implicit operator CancellationToken(DisposableCancellationToken cancellationToken) => cancellationToken.Token;
        }
    }
}
