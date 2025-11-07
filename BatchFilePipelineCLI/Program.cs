using BatchFilePipelineCLI.Utility.ID;
using System.Globalization;

namespace BatchFilePipeline
{
    /// <summary>
    /// Handle the program entry for the CLI processing pipeline
    /// </summary>
    internal class Program
    {
        /*----------Functions----------*/
        //PRIVATE

        /// <summary>
        /// Asynchronous entry point for the application that can be used to control the execution of the pipeline steps
        /// </summary>
        /// <param name="args">Command line arguments that have been supplied to the program</param>
        /// <returns>Returns the error code that resulted from the programs operation</returns>
        private static async Task<int> Main(string[] args)
        {
            // This program will needs to be able to be closed when required
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };

            // Testing
            
            Console.Write("How big a chunk (MB)?: ");
            string? chunkSizeString = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(chunkSizeString))
            {
                Console.WriteLine("ERROR: Empty chunk size");
                return -1;
            }
            if (int.TryParse(chunkSizeString, CultureInfo.InvariantCulture, out int chunkSize) == false)
            {
                Console.WriteLine($"ERROR: Received chunk size '{chunkSizeString}' couldn't be parsed");
                return -1;
            }

            string? input = string.Empty;
            while (cancellationTokenSource.IsCancellationRequested == false)
            {
                Console.Write("Enter filepath: ");
                input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) == true)
                {
                    continue;
                }

                var fingerprint = await FingerprintFactory.FingerprintAsync(input, chunkSize, cancellationTokenSource.Token);
                Console.WriteLine($"Fingerprint: {fingerprint}");
            }
            return 0;
        }
    }
}
