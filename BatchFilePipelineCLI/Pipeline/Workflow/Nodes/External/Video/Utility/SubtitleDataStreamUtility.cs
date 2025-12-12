using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Utility
{
    /// <summary>
    /// Provide additional functionality for <see cref="SubtitleDataStream"/>
    /// </summary>
    public static class SubtitleDataStreamUtility
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The common name format that will be used to indicate subtitle data
        /// </summary>
        /// <remarks>
        /// Additional data will be substituted into the name for distinction:
        ///     {0} == Stream Index
        ///     {1} == Codec
        /// </remarks>
        public const string EXPORT_NAME_FORMAT = "Stream{0}";

        /// <summary>
        /// The extension that will be used for standard text based subtitle files
        /// </summary>
        public const string STANDARD_TEXT_SUBTITLE_EXTENSION = "srt";

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the export name without an extension supplied
        /// </summary>
        /// <param name="stream">The stream that is being processed</param>
        /// <returns>Returns just the basic, expected name of the subtitle file expected</returns>
        public static string GetExportNameWithoutExtension(this SubtitleDataStream stream)
        {
            return string.Format(EXPORT_NAME_FORMAT, stream.Index, stream.CodecName);
        }

        /// <summary>
        /// Get the name of the export file that will be used for the specified stream
        /// </summary>
        /// <param name="stream">The Subtitle stream object that is being processed</param>
        /// <returns>Returns a file name that can be used to reference this subtitle stream as a file</returns>
        public static string GetExportName(this SubtitleDataStream stream)
        {
            return GetExportName(stream, GetCodecExtension(stream));
        }

        /// <summary>
        /// Get the name of the disk file for subtitles that are to be used for the specified stream
        /// </summary>
        /// <param name="stream">The subtitle stream object that is being processed</param>
        /// <param name="extension">The extension that is to be used for the output element, including the '.' E.g. ".srt"</param>
        /// <returns>Returns a file name that can be used to reference this subtitle stream as a file</returns>
        public static string GetExportName(this SubtitleDataStream stream, string extension)
        {
            return $"{GetExportNameWithoutExtension(stream)}{extension}";
        }

        /// <summary>
        /// Try to find a subtitle file that matches the 
        /// </summary>
        /// <param name="stream">The stream that is being looked for as a single file</param>
        /// <param name="directory">The directory within which to search for the subtitle file</param>
        /// <param name="filePath">Passes out the file path for the subtitle that matches the stream</param>
        /// <returns>Returns true if was able to find a subtitle that matches the stream</returns>
        public static bool TryFindSubtitleFile(this SubtitleDataStream stream, string directory, [NotNullWhen(true)] out string? filePath)
        {
            return TryFindSubtitleFile(stream, directory, GetCodecExtension(stream), out filePath);
        }

        /// <summary>
        /// Try to find a subtitle file that matches the 
        /// </summary>
        /// <param name="stream">The stream that is being looked for as a single file</param>
        /// <param name="directory">The directory within which to search for the subtitle file</param>
        /// <param name="extension">The expected extension for the file that is being searched for</param>
        /// <param name="filePath">Passes out the file path for the subtitle that matches the stream</param>
        /// <returns>Returns true if was able to find a subtitle that matches the stream</returns>
        public static bool TryFindSubtitleFile(this SubtitleDataStream stream, string directory, string extension, [NotNullWhen(true)] out string? filePath)
        {
            string fileName = GetExportNameWithoutExtension(stream);
            filePath = Directory.EnumerateFiles(directory, $"*{extension}", SearchOption.TopDirectoryOnly)
                .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith(fileName))
                .FirstOrDefault();
            return string.IsNullOrWhiteSpace(filePath) == false;
        }

        /// <summary>
        /// Retrieve the expected subtitle extension type for the specified codec name
        /// </summary>
        /// <param name="stream">The Subtitle stream object that is being processed</param>
        /// <returns>Returns the extension expected for the extracted codec data</returns>
        /// <exception cref="ArgumentException">Thrown if there is an unknown or unexpected codec supplied</exception>
        /// <exception cref="ArgumentNullException">There is no codec defined for the stream to be tested</exception>
        public static string GetCodecExtension(this SubtitleDataStream stream)
        {
            if (string.IsNullOrWhiteSpace(stream.CodecName) == true)
            {
                throw new ArgumentNullException($"[{nameof(SubtitleDataStreamUtility)}] No codec supplied for getting type extension");
            }
            return stream.CodecName switch
            {
                "subrip"            => ".srt",
                "ass"               => ".ass",
                "ssa"               => ".ssa",
                "webvtt"            => ".vtt",
                "text"              => ".srt",
                "dvd_subtitle"      => ".mkv",
                "hdmv_pgs_subtitle" => ".sup",
                "xsub"              => ".sub",
                _ => throw new ArgumentException($"[{nameof(SubtitleDataStreamUtility)}] Unexpected subtitle code type '{stream.CodecName}'")
            };
        }
    }
}
