using System.Text;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control.Utility
{
    /// <summary>
    /// Provide some extension functionality to make it easier when working with the console
    /// </summary>
    internal static class ConsoleUtility
    {
        /*----------Functions----------*/
        //PUBLIC

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
