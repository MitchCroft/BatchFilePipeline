using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.External;
using System.Collections;
using System.Text;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFmpeg
{
    /// <summary>
    /// Handle the process of replacing a collection of subtitle streams in a video file
    /// </summary>
    [PipelineNode(nameof(ReplaceSubtitleStreamsNode), NodeUsage.Process)]
    internal sealed class ReplaceSubtitleStreamsNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need a lot of information to know how to process this action
        /// </summary>
        private readonly Property _executableProperty = Property.Create
        (
            "FFmpeg",
            "The path or name of the ffmpeg executable that will be run when processing this Node",
            typeof(string),
            "Path/To/Executable/ffmpeg.exe"
        );
        private readonly Property _sourcePathProperty = Property.Create
        (
            "SourcePath",
            "The path to the source video file that is going to be replaced",
            typeof(string),
            "Path/To/Video.mp4"
        );
        private readonly Property _fileInfoProperty = Property.Create
        (
            "TargetInfo",
            $"The {nameof(VideoDetails)} object that describes the video file that is going to update the subtitle streams",
            typeof(VideoDetails)
        );
        private readonly Property _streamsProperty = Property.Create
        (
            "Streams",
            $"The collection of {nameof(SubtitleDataStream)}'s that are going to be replaced in the final output",
            typeof(IEnumerable)
        );
        private readonly Property _externalSubtitleDirProperty = Property.Create
        (
            "ExternalSubtitleDir",
            "The directory that contains the collection of subtitle files that can be sampled for inclusion in the remuxing process",
            typeof(string),
            "Path/To/Files/"
        );
        private readonly Property _subtitleFilesExtensionProperty = Property.Create
        (
            "SubtitleFilesExtension",
            "The extension that is to be looked for on the subtitle files within the external directory for use within the remuxing process",
            SubtitleDataStreamUtility.STANDARD_TEXT_SUBTITLE_EXTENSION,
            "E.g. srt"
        );
        private readonly Property _outputPathProperty = Property.Create
        (
            "OutputPath",
            "The path that will be used for the output file that results from this replacement operation",
            typeof(string),
            "Path/To/Output/File.mp4"
        );

        /// <summary>
        /// We can pass out the path to the file so that it can be used in later stages
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The path to the resulting video file that has had its subtitle streams replaced",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_executableProperty, _sourcePathProperty, _fileInfoProperty, _streamsProperty, _externalSubtitleDirProperty, _subtitleFilesExtensionProperty, _outputPathProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(IReadOnlyDictionary<string, object?> inputs,
                                                                       CancellationToken cancellationToken)
        {
            // Retrieve the information that will be used for processing
            string executable = (string)inputs[_executableProperty.Name]!;
            string sourcePath = (string)inputs[_sourcePathProperty.Name]!;
            VideoDetails targetInfo = (VideoDetails)inputs[_fileInfoProperty.Name]!;
            IEnumerable streams = (IEnumerable)inputs[_streamsProperty.Name]!;
            string externalSubtitleDir = (string)inputs[_externalSubtitleDirProperty.Name]!;
            string subtitleFilesExtension = (string)inputs[_subtitleFilesExtensionProperty.Name]!;
            string outputPath = (string)inputs[_outputPathProperty.Name]!;

            // We need to identify the collection of files that will be included in this remuxing
            List<string> filesToCombine = new List<string> { sourcePath };

            // Try to find the subtitle streams that we're going to be using in this process
            List<(SubtitleDataStream stream, string subtitlePath)> subtitleStreams = new();
            foreach (var stream in streams)
            {
                if (stream is not SubtitleDataStream subtitleStream)
                {
                    Logger.Log($"[{nameof(ReplaceSubtitleStreamsNode)}] Unable to process '{stream}', it's not a {nameof(SubtitleDataStream)}");
                    continue;
                }

                // Look for the subtitle file that will be used for this stream
                if (subtitleStream.TryFindSubtitleFile(externalSubtitleDir, $".{subtitleFilesExtension}", out var externalSubtitleFilePath) == false)
                {
                    return new ExecutionResult(404, $"[{nameof(ReplaceSubtitleStreamsNode)}] Unable to find the external subtitle file in '{externalSubtitleDir}' for one of the streams being replaced");
                }

                // Add the file to the collection of streams we're managing
                subtitleStreams.Add((subtitleStream, externalSubtitleFilePath));
                filesToCombine.Add(externalSubtitleFilePath);
            }

            // We can start constructing our argument for use. We need to include all of the external files
            StringBuilder argument = new StringBuilder($"{string.Join(" ", filesToCombine.Select(x => $"-i \"{x}\""))} ");

            // We want to preserve all of the streams not being replaced
            var preservedStreams = targetInfo.Streams
                .Where(x => subtitleStreams.Any(y => x == y.stream) == false)
                .ToArray();
            argument.Append($"{string.Join(" ", preservedStreams.Select(x => $"-map 0:{x.Index}"))} ");

            // We can copy in all of the new subtitle streams as well
            argument.Append($"{string.Join(" ", subtitleStreams.Select((_, i) => $"-map {i + 1}:0"))} -c:v copy -c:a copy ");

            // Count how many existing subtitle streams exist outside of those being replaced
            int persistingSubtitlesCount = preservedStreams.Count(x => x.StreamType == StreamType.Subtitle);

            // Force the encoding for the subtitle streams
            argument.Append($"{string.Join(" ", subtitleStreams.Select((x, i) => $"-c:s:{persistingSubtitlesCount + i} {Path.GetExtension(x.subtitlePath).Substring(1)}"))} ");

            // We can add all of the meta data that did exist on the previous streams
            var metaDataInserts = subtitleStreams
                .Select((x, i) => (x.stream, i))
                .Where(x => (x.stream.Tags?.Count ?? 0) != 0)
                .Select(x => string.Join(" ", x.stream.Tags!.Select(y => $"-metadata:s:{preservedStreams.Length + x.i} {y.Key}={y.Value}")));
            argument.Append($"{string.Join(" ", metaDataInserts)} ");

            // Finally, we have the output path for the file we want to use
            argument.Append($"\"{outputPath}\"");

            // Make sure the output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            // We can run the remuxing process now
            var result = await ExternalProcess.RunAsync
            (
                executable,
                argument.ToString(),
                onOutput: msg => Logger.Log($"[{nameof(ReplaceSubtitleStreamsNode)}] {msg}"),
                onError: msg => Logger.Log($"[{nameof(ReplaceSubtitleStreamsNode)}] {msg}"),
                cancellationToken: cancellationToken
            );
            if (cancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }

            // If there was a problem, we're in trouble
            if (result.DidError == true)
            {
                return new ExecutionResult(result.ExitCode, result.ToString());
            }
            Logger.Success($"[{nameof(ReplaceSubtitleStreamsNode)}] Replaced subtitles in '{targetInfo}'\n\t{string.Join("\n\t", subtitleStreams.Select(x => $"{x.stream.Index} -> {x.subtitlePath}"))}");

            // We have completed successfully
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, outputPath }
                }
            );
        }
    }
}
