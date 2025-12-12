using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.External;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFmpeg
{
    /// <summary>
    /// Handle the extraction of subtitles from a collection of streams
    /// </summary>
    [PipelineNode(nameof(ExtractSubtitlesNode), NodeUsage.Process)]
    internal sealed class ExtractSubtitlesNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There will need to be a series of properties that are used to process the processing of data in this process
        /// </summary>
        private readonly Property _executableProperty = Property.Create
        (
            "FFmpeg",
            "The path or name of the ffmpeg executable that will be run when processing this Node",
            typeof(string),
            "Path/To/Executable/ffmpeg.exe"
        );
        private readonly Property _sourceProperty = Property.Create
        (
            "Source",
            "The path to the file where the stream information can be received and processed",
            typeof(string),
            "Path/To/Video.mp4"
        );
        private readonly Property _streamsProperty = Property.Create
        (
            "Streams",
            "The collection of streams that are to be extracted as files from the process",
            typeof(IEnumerable)
        );
        private readonly Property _storageProperty = Property.Create
        (
            "StorageDir",
            "The location on disc where the generated files should be located. The file outputs will be labelled based on their stream index",
            typeof(string)
        );
        private readonly Property _analyseDurationProperty = Property.Create
        (
            "AnalyseDuration",
            "The amount of data that can be processed when analysing the file for stream information",
            "100M",
            "The size of memory allowed, e.g.: 500K, 2M, 1G"
        );
        private readonly Property _probeSizeProperty = Property.Create
        (
            "ProbeSize",
            "The amount of data that can be processed when probing the file for stream information",
            "100M",
            "The size of memory allowed, e.g.: 500K, 2M, 1G"
        );

        /// <summary>
        /// As an output, we can deliver the collection of file paths to the extracted files that were created
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "A collection of the file paths to the extracted subtitle files that can be used",
            typeof(IEnumerable<string>)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_executableProperty, _sourceProperty, _streamsProperty, _storageProperty, _analyseDurationProperty, _probeSizeProperty];

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
            // We're going to try and run an external process, anything could happen
            try
            {
                // Retrieve the information that is needed
                string executable = (string)inputs[_executableProperty.Name]!;
                string source = (string)inputs[_sourceProperty.Name]!;
                IEnumerable streams = (IEnumerable)inputs[_streamsProperty.Name]!;
                string storageDir = (string)inputs[_storageProperty.Name]!;
                string analyseDuration = (string)inputs[_analyseDurationProperty.Name]!;
                string probeSize = (string)inputs[_probeSizeProperty.Name]!;

                // We need a collection of the extracted subtitles from the input file
                List<string> extractedFiles = new();
                foreach (var stream in streams)
                {
                    // Check that this is a subtitle stream that can be processed
                    if (stream is not SubtitleDataStream subtitleStream)
                    {
                        Logger.Log($"[{nameof(ExtractSubtitlesNode)}] Unable to process '{stream}', it's not a {nameof(SubtitleDataStream)}");
                        continue;
                    }

                    // Get the output path for this stream
                    string outputPath = Path.Combine(storageDir, subtitleStream.GetExportName());
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                    // We want to run the process to retrieve the subtitle data from the target
                    var result = await ExternalProcess.RunAsync
                    (
                        executable,
                        $"-analyzeduration {analyseDuration} -probesize {probeSize} -i \"{source}\" -map 0:{subtitleStream.Index} -c copy \"{outputPath}\"",
                        onOutput: msg => Logger.Log($"[{nameof(ExtractSubtitlesNode)}] {msg}"),
                        onError: msg => Logger.Log($"[{nameof(ExtractSubtitlesNode)}] {msg}"),  // Normal logs coming through the error output
                        cancellationToken: cancellationToken
                    );
                    if (cancellationToken.IsCancellationRequested == true)
                    {
                        return new ExecutionResult();
                    }

                    // Check if there was a problem
                    if (result.DidError)
                    {
                        return new ExecutionResult(result.ExitCode, result.ToString());
                    }

                    // Check that we have a file in the expected location
                    if (subtitleStream.TryFindSubtitleFile(storageDir, out _) == false)
                    {
                        return new ExecutionResult(404, $"[{nameof(ExtractSubtitlesNode)}] Processed stream '{subtitleStream}' but was unable to generate an output file in '{storageDir}'");
                    }

                    // We have successfully extracted this subtitle file
                    Logger.Success($"[{nameof(ExtractSubtitlesNode)}] Successfully extracted subtitles to '{outputPath}'");
                    extractedFiles.Add(outputPath);
                }

                // We have our extracted subtitle files that can be used
                Logger.Log($"[{nameof(ExtractSubtitlesNode)}] Extracted {extractedFiles.Count} subtitle files{(extractedFiles.Count > 0 ? $"\n\t{string.Join("\n\t", extractedFiles)}" : string.Empty)}");

                // We have the final result from processing
                return new ExecutionResult
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, extractedFiles }
                    }
                );
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return new ExecutionResult(ex); }
        }
    }
}
