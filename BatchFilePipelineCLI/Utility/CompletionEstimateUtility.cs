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
        /// Try to identify the remaining time until a process is complete based on a log line that contains a percentage complete value
        /// </summary>
        /// <param name="startTime">Cached start time of the operation that can be referenced to determine remaining duration</param>
        /// <param name="logLine">The log line that is to be tested when determining progression</param>
        /// <param name="remainingDuration">Passes out the remaining duration if able to determine if from the specfiied log</param>
        /// <returns>Returns true if the remaining time is valid for use</returns>
        public static bool TryGetRemainingDuration(ref DateTime? startTime,
                                                   string logLine,
                                                   out TimeSpan remainingDuration,
                                                   ref double percentage)
        {
            // By default we don't have any output
            remainingDuration = TimeSpan.Zero;

            // See if we can find a percentage identifier in the log line
            Match match = PERCENTAGE_IDENT.Match(logLine);
            if (match.Success == false)
            {
                return false;
            }

            // Try to parse the percentage value that was found
            if (double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var newPercentage) == false)
            {
                return false;
            }

            // If there is no current start time, we have it now
            if (startTime.HasValue == false)
            {
                startTime = DateTime.Now;
                return false;
            }

            // If the percentage dropped, reset the start time
            if (newPercentage < percentage)
            {
                percentage = newPercentage;
                startTime = null;
                return false;
            }
            percentage = newPercentage;

            // If we don't have any progress, we can't estimate anything
            if (percentage <= double.Epsilon)
            {
                return false;
            }

            // Work out the elapsed time and estimate the remaining duration
            double normalised = Math.Min(1.0, Math.Max(0.0, percentage / 100.0));
            var elapsed = DateTime.Now - startTime.Value;
            double elapsedPerPoint = elapsed.TotalMilliseconds / normalised;
            remainingDuration = TimeSpan.FromMilliseconds(elapsedPerPoint * (1.0 - normalised));
            return true;
        }
    }
}
