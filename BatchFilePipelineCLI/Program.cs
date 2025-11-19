using System.Collections;

using DotNetEnv;

using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Utility.Preserve;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using BatchFilePipelineCLI.Pipeline.Workflow;
using BatchFilePipelineCLI.DynamicProperties;

namespace BatchFilePipelineCLI
{
    /// <summary>
    /// Handle the program entry for the CLI processing pipeline
    /// </summary>
    internal static class Program
    {
        /*----------Variables----------*/
        //CONST

        /// <summary>
        /// The character that is being looked for as a marker of a pipeline value that can be used for processing
        /// </summary>
        private const char ARGUMENT_MARKER = '-';

        /// <summary>
        /// The type of logs that will be output by the program
        /// </summary>
        private static readonly Property LOG_TYPE = Property.Create
        (
            "logType",
            "Defines the type of logs that will be output by the program",
            LogType.Success,
            string.Join(", ", Enum.GetNames<LogType>())
        );

        /// <summary>
        /// Used to define the file that log information should be output to for testing
        /// </summary>
        private static readonly Property LOG_FILE_OUTPUT = Property.Create
        (
            "logFile",
            "Defines a file on disk where log information can be output as well for later review",
            string.Empty,
            "Path/To/File.log"
        );

        /// <summary>
        /// The path to the pipeline description file that is to be processed
        /// </summary>
        private static readonly Property PIPELINE_ARGUMENT = Property.Create
        (
            "pipeline",
            "The path to the pipeline description file that is to be processed",
            string.Empty,
            "Path/To/Pipeline.xml"
        );

        /*----------Functions----------*/
        //PRIVATE

        /// <summary>
        /// Asynchronous entry point for the application that can be used to control the execution of the pipeline steps
        /// </summary>
        /// <param name="args">Command line arguments that have been supplied to the program</param>
        /// <returns>Returns the error code that resulted from the programs operation</returns>
        private static async Task<int> Main(string[] args)
        {
            // Ensure all the reflection types are marked as used
            TypePreserver.Init();

            // This program will needs to be able to be closed when required
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };

            // We can have a general catch all in case something goes wrong
            try
            {
                // Parse the arguments into the different elements that will be needed for testing
                var argumentVariables = ParseArgumentVariables(args);
                ParseLoggerArguments(argumentVariables);

                // Output the argument variables now that the initial logging state has been applied
                Logger.Log($"Parsed argument variables ({argumentVariables.Count}):\n\t{string.Join("\n\t", argumentVariables.Select((v, i) => $"{i}.\t{v.Key}={v.Value}"))}");

                // How this program is going to operate will depend on the operation marker that is specified
                if (ArgumentResolver.TryResolveEnvironmentVariable(PIPELINE_ARGUMENT, argumentVariables, out string? pipelinePath) == true &&
                    string.IsNullOrWhiteSpace(pipelinePath) == false)
                {
                    return await ProcessPipelineAsync(pipelinePath, argumentVariables, cancellationTokenSource.Token);
                }

                // TODO: Other operation markers can be defined

                // If we made it to here, we couldn't find an operation marker for use
                else
                {
                    Logger.Error($"Failed to find an operation marker in the command line arguments, please specify one");
                    return -1;
                }
            }

            // If anything goes wrong, that's a problem
            catch (Exception ex)
            {
                Logger.Exception("Un unexpected exception occurred when processing data", ex);
                return ex.HResult;
            }
        }

        /// <summary>
        /// Handle the process of actioning a pipeline that has been assigned to the program for use
        /// </summary>
        /// <param name="pipelinePath">The path to the pipeline asset that is to be parsed and executed</param>
        /// <param name="argumentVariables">The collection of argument variables that were passed to the program</param>
        /// <param name="cancellationToken">Cancellation token for the process that is to be respected</param>
        /// <returns>Returns the exit code for the running process</returns>
        private static async Task<int> ProcessPipelineAsync(string pipelinePath, Dictionary<string, string?> argumentVariables, CancellationToken cancellationToken)
        {
            // Try to read the pipeline file that is to be processed for testing
            if (PipelineDescription.TryOpen(pipelinePath, out var pipelineDescription) == false)
            {
                Logger.Error($"Failed to open the pipeline file '{pipelineDescription}' for processing");
                return -1;
            }

            // Read in the environment variables that can be used for executing the workflow
            var environmentVariables = LoadEnvironmentVariables();

            // Combine all of the environment variable sources together for the final collection that will be used
            var pipelineEnvironmentVariables = environmentVariables
                .Concat(pipelineDescription.Environment)
                .ToDictionary(x => x.Key, x => x.Value);
            Logger.Log($"Pipeline Environment Variable Set ({pipelineEnvironmentVariables.Count}):\n\t{string.Join("\n\t", pipelineEnvironmentVariables.Select((v, i) => $"{i}.\t{v.Key}={v.Value}"))}");

            // Try to load the library of nodes that are available for use in the pipeline
            var nodeLibrary = new NodeLibrary();
            if (nodeLibrary.TryLoadFromAppDomain() == false)
            {
                Logger.Error($"Unable load the Node Library for processing. Resolve errors and try again");
                return -1;
            }
            Logger.Log($"Loaded node library:\n\t{string.Join("\n\t", nodeLibrary.GetNodeTypes().OrderBy(x => x.characteristics.UsageFlags).ThenBy(x => x.characteristics.TypeID).Select(x => $"{x.nodeType.Name}\n\t\tID={x.characteristics.TypeID}\n\t\tUsageFlags={x.characteristics.UsageFlags}\n\t\tIsShared={x.characteristics.IsShared}"))}");

            // Create the workflow that will be processed to perform the operations required
            Workflow workflow = new Workflow();
            if (workflow.TryLoadFromDescription(pipelineDescription.Workflow, nodeLibrary, pipelineEnvironmentVariables, argumentVariables) == false)
            {
                Logger.Error($"Failed to load the workflow graphs for processing. Resolve errors and try again");
                return -1;
            }

            // Execute the workflow to process the required elements
            return await workflow.ExecuteAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieve all of the environment variables that are defined for operation
        /// </summary>
        /// <returns>Returns the collection of environment variables that are to be processed</returns>
        private static Dictionary<string, string?> LoadEnvironmentVariables()
        {
            // Load any external .env file definitions that are needed for processing
            Env.Load();

            // Retrieve all of the environment variable values for processing
            var availableValues = Environment.GetEnvironmentVariables();
            Dictionary<string, string?> environmentValues = new(availableValues.Count);
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
            return environmentValues;
        }

        /// <summary>
        /// Parse the command line arguments into a collection that can be processed
        /// </summary>
        /// <param name="args">The collection of arguments that were supplied to the program for processing</param>
        /// <returns>Returns the collection of variables that will be used for processing</returns>
        private static Dictionary<string, string?> ParseArgumentVariables(string[] args)
        {
            Dictionary<string, string?> argumentVariables = new(args.Length);
            for (int i = 0; i < args.Length; ++i)
            {
                // The argument should begin with the '-' marker character for use
                if (args[i].StartsWith(ARGUMENT_MARKER) == false)
                {
                    Logger.Error($"Unexpected command line argument '{args[i]}'");
                    continue;
                }

                // We have the key for this value
                string key = args[i].Substring(1);

                // If the next value is marker or if this is the last, then we just use this as a flag
                if (i == args.Length - 1 || args[i + 1].StartsWith(ARGUMENT_MARKER) == true)
                {
                    argumentVariables[key] = true.ToString();
                    continue;
                }

                // Whatever value is next in the argument list is assigned to this key
                ++i;
                argumentVariables[key] = args[i];
            }
            return argumentVariables;
        }

        /// <summary>
        /// Parse the supplied collection of arguments to see how it should apply to the logging
        /// </summary>
        /// <param name="arguments">The collection of arguments that are available for use</param>
        private static void ParseLoggerArguments(Dictionary<string, string?> arguments)
        {
            // Log file output
            if (ArgumentResolver.TryResolveEnvironmentVariable(LOG_FILE_OUTPUT, arguments, out string? logFileOutput) == true &&
                string.IsNullOrWhiteSpace(logFileOutput) == false)
            {
                Logger.RemoveAll(x => x is FileLogOutput);
                Logger.AddLogger(new FileLogOutput(logFileOutput));
            }

            // Log Level
            if (ArgumentResolver.TryResolveEnvironmentVariable(LOG_TYPE, arguments, out LogType logType) == false)
            {
                return;
            }

            // We need to check if the type is valid for use
            if (logType != Logger.LogLevel)
            {
                Logger.LogLevel = logType;
                Logger.Log($"Adjusting log level to '{logType}'");
            }
        }
    }
}
