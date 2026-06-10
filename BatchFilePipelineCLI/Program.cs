using System.Collections;

using DotNetEnv;

using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Utility.Preserve;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Cancellation;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.Utility.Disposal;

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
            // Ensure the foundational elements are created for use
            using var disposal = DisposalBackup.Init();
            TypePreserver.Init();
            CancellationStack.Register();

            // We can have a general catch all in case something goes wrong
            try
            {
                // Parse the arguments into the different elements that will be needed for testing
                var argumentVariables = ParseArgumentVariables(args);
                ParseLoggerArguments(argumentVariables);

                // Output the argument variables now that the initial logging state has been applied
                Logger.Log($"Parsed argument variables ({argumentVariables.Count}):\n\t{string.Join("\n\t", argumentVariables.Select((v, i) => $"{i}.\t{v.Key}={v.Value}"))}");

                // How this program is going to operate will depend on the operation marker that is specified
                if (Resolver.TryResolveEnvironmentVariable(PIPELINE_ARGUMENT, argumentVariables, out string? pipelinePath) == true &&
                    string.IsNullOrWhiteSpace(pipelinePath) == false)
                {
                    using var token = CancellationStack.PushSource();
                    return await ProcessPipelineAsync(pipelinePath, argumentVariables, token);
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
        private static async Task<int> ProcessPipelineAsync(string pipelinePath,
                                                            IReadOnlyDictionary<string, string> argumentVariables,
                                                            CancellationToken cancellationToken)
        {
            // Read in the environment variables that can be used for executing the workflow
            var environmentVariables = LoadEnvironmentVariables();

            // Try to load the library of nodes that are available for use in the pipeline
            var nodeLibrary = new Pipeline.Nodes.NodeLibrary();
            if (nodeLibrary.TryLoadFromAppDomain() == false)
            {
                Logger.Error($"Unable load the Node Library for processing. Resolve errors and try again");
                return -1;
            }
            Logger.Log($"Loaded node library:\n\t{string.Join("\n\t", nodeLibrary.GetNodeTypes().OrderBy(x => x.nodeType.Name).Select(x => $"{x.nodeType.Name}\n\t\tID={x.nodeType.Name}\n\t\tIsShared={x.characteristics.IsShared}"))}");

            // Create the runner that will be used to process the graph operation
            PipelineRunner runner = new PipelineRunner(nodeLibrary, environmentVariables, argumentVariables);
            return await runner.ExecuteMainAsync(pipelinePath, Environment.CurrentDirectory, cancellationToken);
        }

        /// <summary>
        /// Retrieve all of the environment variables that are defined for operation
        /// </summary>
        /// <returns>Returns the collection of environment variables that are to be processed</returns>
        private static IReadOnlyDictionary<string, string> LoadEnvironmentVariables()
        {
            // Load any external .env file definitions that are needed for processing
            Env.Load();

            // Retrieve all of the environment variable values for processing
            var availableValues = Environment.GetEnvironmentVariables();
            Dictionary<string, string> environmentValues = new(availableValues.Count);
            foreach (DictionaryEntry entry in availableValues)
            {
                // Skip any null values
                string? key = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(key) == true)
                {
                    continue;
                }
                environmentValues[key] = entry.Value?.ToString() ?? string.Empty;
            }
            return environmentValues;
        }

        /// <summary>
        /// Parse the command line arguments into a collection that can be processed
        /// </summary>
        /// <param name="args">The collection of arguments that were supplied to the program for processing</param>
        /// <returns>Returns the collection of variables that will be used for processing</returns>
        private static IReadOnlyDictionary<string, string> ParseArgumentVariables(string[] args)
        {
            Dictionary<string, string> argumentVariables = new(args.Length);
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
        private static void ParseLoggerArguments(IReadOnlyDictionary<string, string> arguments)
        {
            // Log file output
            if (Resolver.TryResolveEnvironmentVariable(LOG_FILE_OUTPUT, arguments, out string? logFileOutput) == true &&
                string.IsNullOrWhiteSpace(logFileOutput) == false)
            {
                Logger.RemoveAll(x => x is FileLogOutput);
                Logger.AddLogger(new FileLogOutput(logFileOutput));
            }

            // Log Level
            if (Resolver.TryResolveEnvironmentVariable(LOG_TYPE, arguments, out LogType logType) == false)
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
