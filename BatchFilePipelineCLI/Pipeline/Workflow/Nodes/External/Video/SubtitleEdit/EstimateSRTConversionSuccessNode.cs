using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.SubtitleEdit.Data;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.SubtitleEdit
{
    /// <summary>
    /// Estimate the successful conversion of an SRT subtitle file based on a dictionary of words that can be comparsed against
    /// </summary>
    [PipelineNode(nameof(EstimateSRTConversionSuccessNode), NodeUsage.Process)]
    internal sealed class EstimateSRTConversionSuccessNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There needs to be a series of properties that can be evaluated in this process to determine the success of the operation
        /// </summary>
        private readonly Property _streamsProperty = Property.Create
        (
            "Streams",
            "The collection of streams whose srt subtitle files are to be evaluated in this Node",
            typeof(IEnumerable)
        );
        private readonly Property _sourceDirProperty = Property.Create
        (
            "SourceDir",
            "The location on disk where the subtitle files can be found for evaluation",
            typeof(string),
            "Path/To/Directory/"
        );
        private readonly Property _dictionaryFileProperty = Property.Create
        (
            "DictionaryFile",
            "The file path that will be loaded as a dictionary of different words that will be considered valid for this evaluation. The dictionary is expected to be a plain text file with a different word on every line",
            typeof(string),
            "Path/To/Dictionary.txt"
        );

        /// <summary>
        /// There are several different statistics information that we can receive from this Node after processing
        /// </summary>
        private readonly Property _averageSuccessProperty = Property.Create
        (
            "AverageSuccess",
            "The average success rate for all of the streams that were processed",
            typeof(float)
        );
        private readonly Property _minSuccessProperty = Property.Create
        (
            "MinSuccess",
            "The minimum success rate for all of the streams that were processed",
            typeof(float)
        );
        private readonly Property _maxSuccessProperty = Property.Create
        (
            "MaxSuccess",
            "The maximum success rate for all of the streams that were processed",
            typeof(float)
        );


        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_streamsProperty, _sourceDirProperty, _dictionaryFileProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_averageSuccessProperty, _minSuccessProperty, _maxSuccessProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(IReadOnlyDictionary<string, object?> inputs,
                                                                 CancellationToken cancellationToken)
        {
            // Get the properties that will be needed for evaluation
            IEnumerable streams = (IEnumerable)inputs[_streamsProperty.Name]!;
            string sourceDir = (string)inputs[_sourceDirProperty.Name]!;
            string dictionaryPath = (string)inputs[_dictionaryFileProperty.Name]!;

            // Try to read the dictionary data in for use
            HashSet<string> dictionary = [.. File.ReadAllLines(dictionaryPath).Select(x => x.Trim().ToLower())];
            List<SubtitleMarker> markerBuffer = new();

            // We need to track some values for the streams that were processed
            int totalStreams = 0;
            float averageSuccess = 0f;
            float minSuccess = 1f;
            float maxSuccess = 0f;

            // Iterate over all of the streams and see if we can come up with a rough guess on how successful the operation was
            foreach (var stream in streams)
            {
                // Check that this is a subtitle stream that can be processed
                if (stream is not SubtitleDataStream subtitleStream)
                {
                    Logger.Log($"[{nameof(EstimateSRTConversionSuccessNode)}] Unable to process '{stream}', it's not a {nameof(SubtitleDataStream)}");
                    continue;
                }

                // An additional stream being processed
                ++totalStreams;

                // We need to find the subtitle file that can be evaluated for success
                if (subtitleStream.TryFindSubtitleFile(sourceDir, ".srt", out var subtitleFile) == false)
                {
                    Logger.Error($"[{nameof(EstimateSRTConversionSuccessNode)}] Unable to find an '.srt' subtitle file for the stream '{subtitleStream}' in the directory '{sourceDir}'");
                    minSuccess = 0f;
                    continue;
                }

                // We need to try and parse the subtitle file into the different markers that can be used
                if (SRTParser.TryParseFile(subtitleFile, markerBuffer) == false)
                {
                    Logger.Error($"[{nameof(EstimateSRTConversionSuccessNode)}] Failed to parse the file '{subtitleFile}' identified for stream '{subtitleStream}' in the directory '{sourceDir}'");
                    minSuccess = 0f;
                    continue;
                }

                // Determine the percentage of valid words in the resulting markers
                float validMarkers = 0f;
                foreach (var marker in markerBuffer)
                {
                    // If the marker is empty, it's invalid and couldn't identify the correct text
                    if (marker.IsValid == false)
                    {
                        continue;
                    }

                    // Find the different parts of the marker text that is to be processed
                    string[] parts = marker.Text!.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);

                    // Check each part to see if should count towards success
                    float validParts = parts.Length;
                    foreach (var part in parts)
                    {
                        // Isolate the word in this section
                        string word = new string(part.Where(c => char.IsLetterOrDigit(c))
                            .Select(c => char.ToLower(c))
                            .ToArray());
                        
                        // If the dictionary contains the word, we're good
                        if (dictionary.Contains(word) == true)
                        {
                            continue;
                        }

                        // If the word is just a number, that wouldn't be in the dictionary
                        if (double.TryParse(word, out _) == true)
                        {
                            continue;
                        }

                        // This word couldn't be verified, treating as a failure
                        Logger.Log($"[{nameof(EstimateSRTConversionSuccessNode)}] Unknown word '{word}' derived from '{part}'");
                        --validParts;
                    }

                    // We can contribute the percentage of success to the total from this marker
                    validMarkers += validParts / parts.Length;
                }

                // Update the running stats that are needed
                float percentage = validMarkers / markerBuffer.Count;
                averageSuccess += percentage;
                if (percentage < minSuccess)
                {
                    minSuccess = percentage;
                }
                if (percentage > maxSuccess)
                {
                    maxSuccess = percentage;
                }
            }

            // If there were no streams, invalidate all of the return values
            if (totalStreams == 0)
            {
                averageSuccess = 0f;
                minSuccess = 0f;
                maxSuccess = 0f;
            }

            // Otherwise, calculate the average to be used
            else
            {
                averageSuccess /= totalStreams;
            }

            // We have our results from the evaluation
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _averageSuccessProperty.Name, averageSuccess },
                    { _maxSuccessProperty.Name, maxSuccess },
                    { _minSuccessProperty.Name, minSuccess }
                }
            ));
        }
    }
}
