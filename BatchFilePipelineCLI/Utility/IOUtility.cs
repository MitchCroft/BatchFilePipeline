using System.Runtime.CompilerServices;

namespace BatchFilePipelineCLI.Utility
{
    /// <summary>
    /// Provide additional utility functions for processing IO operations
    /// </summary>
    public static class IOUtility
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// Correct a file path to ensure consistent, standard elements
        /// </summary>
        /// <param name="path">The path that is to be cleaned</param>
        /// <returns>Returns the original path with the cleaned up characters</returns>
        public static string CleanFilePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) == true)
            {
                return string.Empty;
            }
            path = path.Replace('\\', '/').Trim();
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path;
        }

        /// <summary>
        /// Retrieve the name of the directory one level up from the provided path
        /// </summary>
        /// <param name="path">The input path that is to be processed</param>
        /// <returns>Returns the directory one level up from the provided path or empty if none</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDirectoryName(string? path) => CleanFilePath(Path.GetDirectoryName(path));

        /// <summary>
        /// Combine the provided paths into a working path
        /// </summary>
        /// <param name="path1">The left side path that will be processed</param>
        /// <param name="path2">The right side path that will be processed</param>
        /// <returns>Returns the resulting combined path for the given values</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Combine(string path1, string path2) => CleanFilePath(Path.Combine(path1, path2));

        /// <summary>
        /// Resolve the provided path into a full path that can be used for processing
        /// </summary>
        /// <param name="path">The input path that is to be resolved against</param>
        /// <returns>Returns the final, full path that can be used for processing</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFullPath(string path) => CleanFilePath(Path.GetFullPath(path));

        /// <summary>
        /// Resolve the provided path into a full path that can be used for processing
        /// </summary>
        /// <param name="path">The input path that is to be resolved against</param>
        /// <param name="root">The root path that should be used as the base for any relative paths supplied</param>
        /// <returns>Returns the final, full path that can be used for processing</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFullPath(string path, string root) => CleanFilePath(Path.GetFullPath(path, root));
    }
}
