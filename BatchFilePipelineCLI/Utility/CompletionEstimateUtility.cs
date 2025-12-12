using System.Globalization;
using System.Text.RegularExpressions;

namespace BatchFilePipelineCLI.Utility
{
    /// <summary>
    /// Provide the functionality for estimating a completion time based on the percentage value in a log line
    /// </summary>
    public static class CompletionEstimateUtility
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Regex pattern that will be used to identify the percentage identifier in a log line
        /// </summary>
        private static readonly Regex PERCENTAGE_IDENT = new Regex(@"(\d+(?:\.\d+)?)%");

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Try to parse a percentage value from a line of text
        /// </summary>
        /// <param name="logLine">The line of text that will be investigated for the percentage value to extract and use</param>
        /// <param name="percentage">Passes out the percentage value if able to find it</param>
        /// <returns>Returns true if was able to retrieve the percentage value</returns>
        public static bool TryParsePercentage(string logLine,
                                              out double percentage)
        {
            // See if we can find a percentage identifier in the log line
            Match match = PERCENTAGE_IDENT.Match(logLine);
            if (match.Success == false)
            {
                percentage = 0;
                return false;
            }

            // Try to parse the percentage value that was found
            return double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out percentage);
        }

        /// <summary>
        /// Try to identify the remaining time until a process is complete based on a log line that contains a percentage complete value
        /// </summary>
        /// <param name="startTime">Cached start time of the operation that can be referenced to determine remaining duration</param>
        /// <param name="logLine">The log line that is to be tested when determining progression</param>
        /// <param name="remainingDuration">Passes out the remaining duration if able to determine if from the specfiied log</param>
        /// <param name="prevPercentage">The previous percentage value that can be used to determine if the counter has restarted</param>
        /// <returns>Returns true if the remaining time is valid for use</returns>
        public static bool TryGetRemainingDuration(ref DateTime? startTime,
                                                   string logLine,
                                                   out TimeSpan remainingDuration,
                                                   ref double prevPercentage)
        {
            remainingDuration = TimeSpan.Zero;
            return TryParsePercentage(logLine, out var parsedPercentage) &&
                   TryGetRemainingDuration(ref startTime, parsedPercentage, out remainingDuration, ref prevPercentage);
        }

        /// <summary>
        /// Try to identify the remaining time until a process is complete based on a percentage complete value
        /// </summary>
        /// <param name="startTime">Cached start time of the operation that can be referenced to determine remaining duration</param>
        /// <param name="newPercentage">The new percentage complete value that will be used when estimating remaining duration</param>
        /// <param name="remainingDuration">Passes out the anticipated remaining duration until the parsing is complete</param>
        /// <param name="prevPercentage">The previous percentage value that can be used to determine if the counter has restarted</param>
        /// <returns>Returns true if the returned estimate is valid for use</returns>
        public static bool TryGetRemainingDuration(ref DateTime? startTime,
                                                   double newPercentage,
                                                   out TimeSpan remainingDuration,
                                                   ref double prevPercentage)
        {
            // By default we don't have any output
            remainingDuration = TimeSpan.Zero;

            // If there is no current start time, we have it now
            if (startTime.HasValue == false)
            {
                startTime = DateTime.Now;
                return false;
            }

            // If the percentage dropped, reset the start time
            if (newPercentage < prevPercentage)
            {
                prevPercentage = newPercentage;
                startTime = null;
                return false;
            }
            prevPercentage = newPercentage;

            // If we don't have any progress, we can't estimate anything
            if (prevPercentage <= double.Epsilon)
            {
                return false;
            }

            // Work out the elapsed time and estimate the remaining duration
            double normalised = Math.Min(1.0, Math.Max(0.0, prevPercentage / 100.0));
            var elapsed = DateTime.Now - startTime.Value;
            double elapsedPerPoint = elapsed.TotalMilliseconds / normalised;
            remainingDuration = TimeSpan.FromMilliseconds(elapsedPerPoint * (1.0 - normalised));
            return true;
        }
    }
}
