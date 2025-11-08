namespace BatchFilePipelineCLI.Logging
{
    /// <summary>
    /// Define the different types of message log that can be recorded
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// General information output that can be displayed
        /// </summary>
        Info,

        /// <summary>
        /// Warning information, something is amiss but it won't stop anything
        /// </summary>
        Warn,

        /// <summary>
        /// Something has gone wrong that can't be recovered from
        /// </summary>
        Error,

        /// <summary>
        /// An unexpected exception has occurred
        /// </summary>
        Exception,

        /// <summary>
        /// An assert has failed to pass and progress is halted
        /// </summary>
        Assert,
    }

    /// <summary>
    /// Interface that describes how logging will be performed when processing the 
    /// </summary>
    public interface ILogOutput
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Request logging the supplied message to the output
        /// </summary>
        /// <param name="message">The message that is to be output to the log</param>
        /// <param name="type">The type of log that this is</param>
        public void LogMessage(object message, LogType type);
    }
}
