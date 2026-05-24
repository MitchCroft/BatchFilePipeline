namespace BatchFilePipelineCLI.Logging
{
    /// <summary>
    /// Provide logging functionality through the standard output targets of the program
    /// </summary>
    internal sealed class StandardLogOutput : ILogOutput
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Use an instance of a generic object as a thread lock for controlling access to standard output
        /// </summary>
        private readonly object _lock = new object();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Request logging the supplied message to the output
        /// </summary>
        /// <param name="message">The message that is to be output to the log</param>
        /// <param name="type">The type of log that this is</param>
        public void LogMessage(object message, LogType type)
        {
            lock (_lock)
            {
                try
                {
                    Console.ForegroundColor = type switch
                    {
                        LogType.Warn => ConsoleColor.Yellow,
                        LogType.Success => ConsoleColor.Green,
                        LogType.Error => ConsoleColor.Red,
                        LogType.Exception => ConsoleColor.Magenta,
                        LogType.Assert => ConsoleColor.Cyan,
                        _ => ConsoleColor.White,
                    };
                    if (type <= LogType.Success)
                    {
                        Console.WriteLine($"[{type}] {message}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"[{type}] {message}");
                    }
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
    }
}
