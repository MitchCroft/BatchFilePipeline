namespace BatchFilePipelineCLI.Utility.Extensions
{
    /// <summary>
    /// Provide extension functions for <see cref="Task"/> elements
    /// </summary>
    internal static class TaskExtensions
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Await the supplied task and ignore any cancelled operation exceptions that arise
        /// </summary>
        /// <param name="task">The task that is to be monitored</param>
        public static async Task SurpressCancellation(this Task task)
        {
            try { await task; }
            catch (TaskCanceledException) {}
        }

        /// <summary>
        /// Await the supplied task and ignore any cancelled operation exceptions that arise
        /// </summary>
        /// <typeparam name="T">The expected result of the task being run</typeparam>
        /// <param name="task">The task that is to be monitored</param>
        /// <param name="defaultValue">The default value that is to be returned if task is cancelled</param>
        /// <returns>Returns the result of the task or the default value if it was cancelled</returns>
        public static async Task<T> SurpressCancellation<T>(this Task<T> task, T defaultValue = default)
        {
            try { return await task; }
            catch (TaskCanceledException) { return defaultValue; }
        }
    }
}
