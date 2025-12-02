namespace BatchFilePipelineCLI.Utility.External
{
    /// <summary>
    /// Contain the result of executing an external process
    /// </summary>
    public readonly struct ProcessResult
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The exit code that was received from the process
        /// </summary>
        public readonly int ExitCode;

        /// <summary>
        /// The standard output that was received from the process
        /// </summary>
        public readonly string StdOut;

        /// <summary>
        /// The error output that was received from the process
        /// </summary>
        public readonly string StdErr;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Flags if the process encountered a problem during execution
        /// </summary>
        public bool DidError => ExitCode != 0;

        /// <summary>
        /// Flag that indicates if the operation was cancelled while processing
        /// </summary>
        public readonly bool WasCancelled { get; }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the process result with the exit code, standard output, and error output
        /// </summary>
        public ProcessResult(int exitCode, string stdOut, string stdErr, bool wasCancelled = false)
        {
            ExitCode = exitCode;
            StdOut = stdOut;
            StdErr = stdErr;
            WasCancelled = wasCancelled;
        }

        /// <summary>
        /// Return a string representation of the process result
        /// </summary>
        public override string ToString()
        {
            return ExitCode == 0 ? "Success" : $"[{ExitCode}] {StdErr}";
        }
    }

}
