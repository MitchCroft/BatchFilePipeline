using BatchFilePipelineCLI.Utility.ID;
using DotNetEnv;
using System.Collections;
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

            int index = 0;
            var environmentVariables = LoadEnvironmentVariables(args);
            Console.WriteLine(string.Join(Environment.NewLine, environmentVariables.Select(x => $"{index++}\t{x.Key}={x.Value}")));
            return 0;

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

        /// <summary>
        /// Find all of the environment variables that have been defined for use on this run through of the program
        /// </summary>
        /// <param name="args">The command line arguments that were supplied to this program initially</param>
        /// <returns>Returns the complete collection of environment variables that are available for use</returns>
        private static Dictionary<string, string> LoadEnvironmentVariables(string[] args)
        {
            // Load any external .env file definitions that are needed for processing
            Env.Load();

            // Retrieve all of the environment variable values for processing
            var availableValues = Environment.GetEnvironmentVariables();

            // Stage 1. Load all of the environment variables that are defined
            Dictionary<string, string?> environmentValues = new(availableValues.Count + args.Length / 2);
            foreach (DictionaryEntry entry in availableValues)
            {
                // Skip any null values
                string? key = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(key) == true)
                {
                    continue;
                }
                environmentValues[key] = entry.Value?.ToString();
            }

            // Stage 2. Load the command line arguments as options that can be used
            const char ENV_MARKER_CHAR = '-';
            for (int i = 0; i < args.Length; ++i)
            {
                // The argument should begin with the '-' marker character for use
                if (args[i].StartsWith(ENV_MARKER_CHAR) == false)
                {
                    Console.Error.WriteLine($"[BFPCLI] Unexpected command line argument '{args[i]}'");
                    continue;
                }

                // We have the key for this value
                string key = args[i].Substring(1);

                // If the next value is marker or if this is the last, then we just use this as a flag
                if (i == args.Length - 1 || args[i + 1].StartsWith(ENV_MARKER_CHAR) == true)
                {
                    environmentValues[key] = true.ToString();
                    continue;
                }

                // Whatever value is next in the argument list is assigned to this key
                ++i;
                environmentValues[key] = args[i];
            }
            return environmentValues;
        }
    }
}
