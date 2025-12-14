using System.Runtime.InteropServices;

namespace BatchFilePipelineCLI.Utility.ExecutionState
{
    /// <summary>
    /// Manage the overall state of execution flags that are applied to the thread
    /// </summary>
    public static class ExecutionStateHandler
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Store the collection of flags that will be applied to the thread
        /// </summary>
        private static readonly List<EXECUTION_STATE> _stateStack = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Push the specified flags onto the execution state stack
        /// </summary>
        /// <param name="flags">The flag mask that should be used</param>
        /// <returns>Returns a marker that can be disposed to clear the state when complete</returns>
        public static StateMarker Push(EXECUTION_STATE flags = EXECUTION_STATE.ES_SYSTEM_REQUIRED)
        {
            _stateStack.Add(flags);
            return new StateMarker(UpdateThreadState());
        }

        /// <summary>
        /// Pop off the last state marker that has been pushed on the stack
        /// </summary>
        public static void Pop()
        {
            if (_stateStack.Count == 0)
            {
                throw new InvalidOperationException($"[{nameof(ExecutionStateHandler)}] Unable to pop execution state, stack is empty");
            }
            _stateStack.RemoveAt(_stateStack.Count - 1);
            UpdateThreadState();
        }

        //PRIVATE

        /// <summary>
        /// Update the thread execution state based on the current stack
        /// </summary>
        /// <remarks>Returns the mask of the current flags that have been set</remarks>
        private static EXECUTION_STATE UpdateThreadState()
        {
            EXECUTION_STATE state = EXECUTION_STATE.ES_CONTINUOUS;
            foreach (var flag in _stateStack)
            {
                state |= flag;
            }
            SetThreadExecutionState(state);
            return state;
        }

        /// <summary>
        /// Function that can be used to adjust the execution state of the thread
        /// </summary>
        /// <param name="esFlags">The flags that are to be applied for the thread</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
    }
}
