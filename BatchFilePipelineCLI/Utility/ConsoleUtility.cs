using System.Text;

namespace BatchFilePipelineCLI.Utility
{
    /// <summary>
    /// Provide some extension functionality to make it easier when working with the console
    /// </summary>
    public static class ConsoleUtility
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Use an anonymous object reference for thread locking
        /// </summary>
        private static readonly object _lock = new object();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Provide gated control over access to console output to prevent race conditions
        /// </summary>
        /// <param name="text">The text that is to be written</param>
        public static void Write(string text)
        {
            lock (_lock)
            {
                Console.Write(text);
            }
        }

        /// <summary>
        /// Provide gated control over access to console output to prevent race conditions
        /// </summary>
        /// <param name="text">The text that is to be written</param>
        public static void WriteLine(string text)
        {
            lock (_lock)
            {
                Console.WriteLine(text);
            }
        }

        /// <summary>
        /// Provide gated control over access to the console to perform a generic action
        /// </summary>
        /// <param name="callback">The callback that is to be performed</param>
        public static void Process(Action callback)
        {
            lock (_lock)
            {
                callback();
            }
        }

        /// <summary>
        /// Allow for the asynchronous entering of text for processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that controls how long the operation lasts</param>
        /// <returns>Returns the entered line or null if cancelled</returns>
        public static async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            StringBuilder buffer = new();
            while (cancellationToken.IsCancellationRequested == false)
            {
                // Check if the user has input waiting
                if (Console.KeyAvailable == false)
                {
                    await Task.Delay(50);
                    continue;
                }

                // Check the key that was pressed
                var key = Console.ReadKey(intercept: false);
                if (key.Key == ConsoleKey.Enter)
                {
                    return buffer.ToString();
                }

                // Return the current display
                buffer.Append(key.KeyChar);
            }

            // Cancelled, nothing to report
            return null;
        }
    }
}
