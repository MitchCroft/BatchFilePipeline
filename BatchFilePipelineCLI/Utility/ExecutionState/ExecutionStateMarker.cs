using System.Runtime.InteropServices;

namespace BatchFilePipelineCLI.Utility.ExecutionState
{
    /// <summary>
    /// Handle the marking of execution state within a using block
    /// </summary>
    public sealed class ExecutionStateMarker : IDisposable
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Apply a set of execution state flags to the current thread
        /// </summary>
        /// <param name="flags">The set of flags to be used for processing</param>
        public ExecutionStateMarker(EXECUTION_STATE flags = EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED)
        {
            SetThreadExecutionState(flags);
        }

        /// <summary>
        /// Clear the execution state flags when disposed
        /// </summary>
        public void Dispose()
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        //PRIVATE

        /// <summary>
        /// Function that can be used to adjust the execution state of the thread
        /// </summary>
        /// <param name="esFlags">The flags that are to be applied for the thread</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        /*----------Types----------*/
        //PUBLIC

        /// <summary>
        /// Markers of the different execution states that can be applied
        /// </summary>
        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }
    }
}
