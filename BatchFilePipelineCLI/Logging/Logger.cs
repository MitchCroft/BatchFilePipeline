namespace BatchFilePipelineCLI.Logging
{
    /// <summary>
    /// Static class that can be used to log elements to the output for processing
    /// </summary>
    internal static class Logger
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Define a collection of loggers that will be used to output recorded information
        /// </summary>
        private static readonly List<ILogOutput> _outputs = new()
        {
            new StandardLogOutput()
        };

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The minimum log level that can be output by the contained elements
        /// </summary>
        public static LogType LogLevel { get; set; } = LogType.Info;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Add an additional logger to the handler
        /// </summary>
        /// <param name="logger">The logger instance that is to be included</param>
        /// <returns>Returns true if the logger was added</returns>
        public static bool AddLogger(ILogOutput logger)
        {
            lock (_outputs)
            {
                if (_outputs.Contains(logger) == true)
                {
                    return false;
                }
                _outputs.Add(logger);
                return true;
            }
        }

        /// <summary>
        /// Remove the specified logger from the internal collection
        /// </summary>
        /// <param name="logger">The logger to be removed</param>
        /// <returns>Returns true if the instance was removed</returns>
        public static bool RemoveLogger(ILogOutput logger)
        {
            lock ( _outputs)
            {
                return _outputs.Remove(logger);
            }
        }

        /// <summary>
        /// Remove all loggers that match a predicate
        /// </summary>
        /// <param name="predicate">Predicate to check if log output instances should be removed</param>
        /// <returns>Returns the number of instances removed</returns>
        public static int RemoveAll(Predicate<ILogOutput> predicate)
        {
            lock (_outputs)
            {
                return _outputs.RemoveAll(predicate);
            }
        }

        /// <summary>
        /// Log a basic message to the output
        /// </summary>
        /// <param name="message">The element that is to be logged for processing</param>
        public static void Log(object message) => LogMessage(message, LogType.Info);

        /// <summary>
        /// Log a warning message to the output
        /// </summary>
        /// <param name="message">The element that is to be logged for processing</param>
        public static void Warning(object message) => LogMessage(message, LogType.Warn);

        /// <summary>
        /// Log an error message to the output
        /// </summary>
        /// <param name="message">The element that is to be logged for processing</param>
        public static void Error(object message) => LogMessage(message, LogType.Error);

        /// <summary>
        /// Log an exception to the output
        /// </summary>
        /// <param name="exception">The exception that occurred that is to be processed</param>
        public static void Exception(Exception exception) => LogMessage(exception, LogType.Exception);

        /// <summary>
        /// Log an exception message to the output
        /// </summary>
        /// <param name="message">The element that is to be logged for processing</param>
        /// <param name="exception">The exception that occurred that is to be processed</param>
        public static void Exception(object message, Exception exception) => LogMessage($"{message}\n{exception}", LogType.Exception);

        /// <summary>
        /// Log an assert message to the output
        /// </summary>
        /// <param name="message">The element that is to be logged for processing</param>
        public static void Assert(object message) => LogMessage(message, LogType.Assert);

        //PRIVATE

        /// <summary>
        /// Request logging the supplied message to the output
        /// </summary>
        /// <param name="message">The message that is to be output to the log</param>
        /// <param name="type">The type of log that this is</param>
        private static void LogMessage(object message, LogType type)
        {
            // Check if the log can be shown
            if (type < LogLevel)
            {
                return;
            }

            // Try to log the message to the display
            lock (_outputs)
            {
                for (int i = 0; i < _outputs.Count; ++i)
                {
                    try { _outputs[i].LogMessage(message, type); }
                    catch {}
                }
            }
        }
    }
}
