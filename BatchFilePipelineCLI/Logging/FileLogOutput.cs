namespace BatchFilePipelineCLI.Logging
{
    /// <summary>
    /// Handle the output of log information to a file on disc for later study
    /// </summary>
    internal sealed class FileLogOutput : ILogOutput
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The file location where the log information should be placed for processing
        /// </summary>
        private readonly string _outputFile;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the file log output with the location of the file to output data to
        /// </summary>
        /// <param name="outputFile">The file path to the file that should have log data appended to it</param>
        public FileLogOutput(string outputFile) => _outputFile = outputFile;

        /// <summary>
        /// Request logging the supplied message to the output
        /// </summary>
        /// <param name="message">The message that is to be output to the log</param>
        /// <param name="type">The type of log that this is</param>
        public void LogMessage(object message, LogType type) => File.AppendAllText(_outputFile, $"{DateTime.Now} [{type}] {message}\n");
    }
}
