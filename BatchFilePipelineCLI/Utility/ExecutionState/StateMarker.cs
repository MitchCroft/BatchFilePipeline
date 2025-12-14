namespace BatchFilePipelineCLI.Utility.ExecutionState
{
    /// <summary>
    /// Handle the marking of execution state within a using block
    /// </summary>
    public readonly struct StateMarker : IDisposable
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The state flags that were set at the time of the marker being created
        /// </summary>
        public readonly EXECUTION_STATE Flags;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Apply a set of execution state flags to the current thread
        /// </summary>
        /// <param name="flags">The set of flags to be used for processing</param>
        public StateMarker(EXECUTION_STATE flags) => Flags = flags;

        /// <summary>
        /// Clear the execution state flags when disposed
        /// </summary>
        public void Dispose() => ExecutionStateHandler.Pop();
    }
}
