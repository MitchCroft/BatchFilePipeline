namespace BatchFilePipelineCLI.Logging
{
    /// <summary>
    /// Define the different types of message log that can be recorded
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// General information output that can be displayed describing a process
        /// </summary>
        Info = 0,

        /// <summary>
        /// Something was processed successfully and we're happy about it
        /// </summary>
        Success = 1,

        /// <summary>
        /// Warning information, something is amiss but it won't stop anything
        /// </summary>
        Warn = 2,

        /// <summary>
        /// Something has gone wrong that can't be recovered from
        /// </summary>
        Error = 3,

        /// <summary>
        /// An unexpected exception has occurred
        /// </summary>
        Exception = 4,

        /// <summary>
        /// An assert has failed to pass and progress is halted
        /// </summary>
        Assert = 5,
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
