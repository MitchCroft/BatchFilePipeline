using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility;
using BatchFilePipelineCLI.Utility.External;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.SubtitleEdit
{
    /// <summary>
    /// Allow for the conversion of graphical based subtitles into text based subtitles
    /// </summary>
    [PipelineNode(nameof(ConvertGraphicSubsToTextNode), NodeUsage.Process)]
    internal sealed class ConvertGraphicSubsToTextNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There will need to be a series of properties that are used to process the conversion of the subtitle data
        /// </summary>
        private readonly Property _executableProperty = Property.Create
        (
            "SubtitleEdit",
            "The path or name of the Subtitle Edit executable that will be run when processing this Node",
            typeof(string),
            "Path/To/Executable/SubtitleEdit.exe"
        );
        private readonly Property _streamsProperty = Property.Create
        (
            "Streams",
            "The collection of streams that are to be identified in the source directory for processing",
            typeof(IEnumerable)
        );
        private readonly Property _sourceDirProperty = Property.Create
        (
            "SourceDir",
            "The location on disk where the image based subtitles can be found for conversion",
            typeof(string),
            "Path/To/Directory/"
        );

        /// <summary>
        /// As an output, we can deliver the collection of file paths to the converted files that were created
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "A collection of the file paths to the converted subtitles files that can be used",
            typeof(IEnumerable<string>)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_executableProperty, _streamsProperty, _sourceDirProperty];

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
            // Retrieve the information that is needed
            string executable = (string)inputs[_executableProperty.Name]!;
            IEnumerable streams = (IEnumerable)inputs[_streamsProperty.Name]!;
            string sourceDir = (string)inputs[_sourceDirProperty.Name]!;

            // We need a collection of the extracted subtitles from the process
            List<string> convertedFiles = new();
            foreach (var stream in streams)
            {
                // Check that this is a subtitle stream that can be processed
                if (stream is not SubtitleDataStream subtitleStream)
                {
                    Logger.Log($"[{nameof(ConvertGraphicSubsToTextNode)}] Unable to process '{stream}', it's not a {nameof(SubtitleDataStream)}");
                    continue;
                }

                // Get the expected path of the converted file for processing
                if (subtitleStream.TryFindSubtitleFile(sourceDir, out var expectedInputFile) == false)
                {
                    throw new NullReferenceException($"[{nameof(ConvertGraphicSubsToTextNode)}] Unable to find the expected source subtitles file in '{sourceDir}'");
                }

                // We can run the process that will perform the conversion process
                DateTime? startTime = null;
                double prevPercentage = 0;
                double bufferPercentage = 0;
                int counter = 0;
                var result = await ExternalProcess.RunAsync
                (
                    executable,
                    $"/convert \"{expectedInputFile}\" \"{SubtitleDataStreamUtility.STANDARD_TEXT_SUBTITLE_EXTENSION}\"",
                    onOutput: msg =>
                    {
                        // If this is not a progress line, it might be other information that is important to be able to reference
                        if (CompletionEstimateUtility.TryGetRemainingDuration(ref startTime, msg, out TimeSpan remaining, ref bufferPercentage) == false)
                        {
                            Logger.Log($"[{nameof(ConvertGraphicSubsToTextNode)}] {msg}");
                            return;
                        }

                        // Limit the number of progress logs that are generated, these can be very frequent
                        ++counter;
                        if (counter % 25 != 0)
                        {
                            return;
                        }

                        // Basic log we can use for record
                        string log = $"[{nameof(ConvertGraphicSubsToTextNode)}] OCR Progression {bufferPercentage.ToString("F2")}% - ETA {remaining} ({DateTime.Now + remaining})";
                        Logger.Log(log);

                        // If we pass a milestone, then we can log that as well
                        int prevBucket = (int)Math.Floor(prevPercentage / 25);
                        int curBucket = (int)Math.Floor(bufferPercentage / 25);
                        prevPercentage = bufferPercentage;
                        if (curBucket > prevBucket)
                        {
                            Logger.Success(log);
                        }
                    },
                    onError: msg => Logger.Error($"[{nameof(ConvertGraphicSubsToTextNode)}] {msg}"),
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

                // Check that we have a file in the expected location
                if (subtitleStream.TryFindSubtitleFile(sourceDir, $".{SubtitleDataStreamUtility.STANDARD_TEXT_SUBTITLE_EXTENSION}", out var outputFile) == false)
                {
                    return new ExecutionResult(404, $"[{nameof(ConvertGraphicSubsToTextNode)}] Processed subtitle file '{expectedInputFile}' but was unable to generate an output file in '{sourceDir}'");
                }

                // We have successfully converted the file
                Logger.Success($"[{nameof(ConvertGraphicSubsToTextNode)}] Successfully converted image based subtitles at '{expectedInputFile}' to text based in '{outputFile}'");
                convertedFiles.Add(outputFile);
            }

            // We have our final collection of converted files
            Logger.Log($"[{nameof(ConvertGraphicSubsToTextNode)}] Converted {convertedFiles.Count} subtitles files{(convertedFiles.Count > 0 ? $"\n\t{string.Join("\n\t", convertedFiles)}" : string.Empty)}");

            // We have the final result from the processing
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, convertedFiles }
                }
            );
        }
    }
}
