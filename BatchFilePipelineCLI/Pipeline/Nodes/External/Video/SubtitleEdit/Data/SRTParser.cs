using System.Globalization;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.Video.SubtitleEdit.Data
{
    /// <summary>
    /// Provide utility functionality that will make it easier to interact with .srt files
    /// </summary>
    public static class SRTParser
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The characters that separate the start and end times in a SRT chapter description
        /// </summary>
        private static readonly string[] SPLIT_MARKER = ["-->"];

        /// <summary>
        /// The format of the timestamp that can be parsed for use
        /// </summary>
        private const string TIMESTAMP_FORMAT = @"hh\:mm\:ss\,fff";

        /*----------Functions----------*/
        //PRIVATE

        /// <summary>
        /// Try to parse the specified input line into the different time values for processing
        /// </summary>
        /// <param name="input">The line that is to be parsed for resulting information</param>
        /// <param name="start">Passes out the determined start time from the input text</param>
        /// <param name="end">Passes out the determined end time from the input text</param>
        /// <returns>Returns true if the time values could be extracted successfully</returns>
        public static bool TryParseTimeRange(string input, out TimeSpan start, out TimeSpan end)
        {
            // Defaults
            start = TimeSpan.Zero;
            end = TimeSpan.Zero;

            // Check that there is text to parse
            if (string.IsNullOrWhiteSpace(input) == true)
            {
                return false;
            }

            // We are expecting to be able to get two parts from this
            string[] parts = input.Split(SPLIT_MARKER, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            // Try to parse the time values
            return TimeSpan.TryParseExact(parts[0].Trim(), TIMESTAMP_FORMAT, CultureInfo.InvariantCulture, out start) &&
                   TimeSpan.TryParseExact(parts[1].Trim(), TIMESTAMP_FORMAT, CultureInfo.InvariantCulture, out end);
        }

        /// <summary>
        /// Try to parse an .srt file from the specified file path
        /// </summary>
        /// <param name="filePath">The path to the file that is to be read and parsed</param>
        /// <param name="markers">Passes out the collection of markers that were found for use</param>
        /// <returns>Returns true if the markers could be parsed successfully from the file</returns>
        public static bool TryParseFile(string filePath, [NotNullWhen(true)] out List<SubtitleMarker> markers)
        {
            markers = new List<SubtitleMarker>();
            return TryParseFile(filePath, markers);
        }

        /// <summary>
        /// Try to parse an .srt file from the specified file and add the entries to the buffer
        /// </summary>
        /// <param name="filePath">The path to the file that is to be read and parsed</param>
        /// <param name="buffer">The buffer that new chapter entries should be added to</param>
        /// <returns>Returns true if the markers could be parsed successfully from the file</returns>
        public static bool TryParseFile(string filePath, IList<SubtitleMarker> buffer)
        {
            // Check that the buffer is write-able
            if (buffer.IsReadOnly == true)
            {
                throw new ArgumentException($"[{nameof(SRTParser)}] Unable to fill read-only buffer with entries");
            }

            // Setup the elements needed for processing
            buffer.Clear();
            StringBuilder sb = new();

            // Read in all the lines of the file so we can step over them
            var lines = File.ReadAllLines(filePath);
            for (int searchProgress = 0; searchProgress < lines.Length; ++searchProgress)
            {
                // We need to find the next line that is just a number as the index marker
                int searchPoint = searchProgress;
                int chapterIndex = -1;
                for (; searchPoint < lines.Length; ++searchPoint)
                {
                    // If we can't pull just a number from the line, it's something else
                    if (int.TryParse(lines[searchPoint], CultureInfo.InvariantCulture, out var foundIndex) == false)
                    {
                        continue;
                    }

                    // We have the *potential* chapter index
                    chapterIndex = searchPoint;
                    break;
                }

                // If we couldn't find a possible chapter index, the file is done
                if (chapterIndex == -1)
                {
                    break;
                }

                // The immediate next line should be the timestamps in the format:
                // 00:01:13,400 --> 00:01:14,913
                if (searchPoint + 1 >= lines.Length)
                {
                    break;
                }
                if (TryParseTimeRange(lines[searchPoint + 1], out var start, out var end) == false)
                {
                    // Continue looking from the next line
                    searchProgress = searchPoint + 1;
                    continue;
                }

                // We can just grab the next lines as the text
                sb.Clear();
                for (searchProgress = searchPoint + 2; searchProgress < lines.Length; ++searchProgress)
                {
                    // If the line is empty, we're done with this chapter
                    if (string.IsNullOrWhiteSpace(lines[searchProgress]) == true)
                    {
                        break;
                    }
                    sb.Append(lines[searchProgress]);
                }

                // We have the marker details for this point
                buffer.Add(new SubtitleMarker(chapterIndex, start, end, sb.ToString()));
            }
            return buffer.Count > 0;
        }
    }
}
