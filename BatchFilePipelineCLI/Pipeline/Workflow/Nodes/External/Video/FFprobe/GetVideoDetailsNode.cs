using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Data;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Data.Streams;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.External;
using Newtonsoft.Json.Linq;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe
{
    /// <summary>
    /// Provide a base type for handling the functionality of programs wrapped in a Node interface
    /// </summary>
    [PipelineNode(nameof(GetVideoDetailsNode), NodeUsage.Process)]
    internal sealed class GetVideoDetailsNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PROTECTED

        /// <summary>
        /// We will need the executable that is to be run as a part of this nodes operation
        /// </summary>
        private readonly Property _executableProperty = Property.Create
        (
            "FFprobe",
            "The path or name of the ffprobe executable that will be run when processing this Node",
            typeof(string),
            "Path/To/Executable/ffprobe.exe"
        );
        private readonly Property _targetProperty = Property.Create
        (
            "Target",
            "The path to the video file that is to have it's meta-data retrieved",
            typeof(string),
            "Path/To/Video.mp4"
        );

        /// <summary>
        /// We're going to pass out an information about with all the specifics for the video file
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out an information about that contains all of the extracted details about a video file",
            typeof(VideoDetails)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [ _executableProperty, _targetProperty ];

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
            // We're going to try and run an external process, and anything could happen
            try
            {
                // Get the properties that will be run
                string executable = (string)inputs["FFprobe"]!;
                string target = (string)inputs["Target"]!;

                // We want to run the process to find the meta data for the target file
                var result = await ExternalProcess.RunAsync
                (
                    executable,
                    $"-v quiet -print_format json -show_format -show_streams -show_chapters -show_data \"{target}\"",
                    onError: msg => Logger.Error($"[{nameof(GetVideoDetailsNode)}] {msg}"),
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

                // We can just log the single output that was received
                Logger.Log($"[{nameof(GetVideoDetailsNode)}] {result.StdOut}");

                // We need to format the received data into a format that we can use with other nodes
                return new ExecutionResult
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, ParseVideoDetails(result.StdOut) }
                    }
                );
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return new ExecutionResult(ex); }
        }

        //PRIVATE

        /// <summary>
        /// Parse the received collection of JSON data and format it into a usable object
        /// </summary>
        /// <param name="detailsJson">The JSON data that was received for processing</param>
        /// <returns>Returns a Video Details object with all the information about the file</returns>
        private static VideoDetails ParseVideoDetails(string? detailsJson)
        {
            // If there is no text, nothing we can do
            if (string.IsNullOrWhiteSpace(detailsJson) == true)
            {
                return new VideoDetails();
            }

            // Handle the parsing of the data that needs processing
            var rootObject = JObject.Parse(detailsJson);

            // Retrieve the root elements that are needed
            if (rootObject.TryGetValue("streams", StringComparison.InvariantCulture, out var streamsToken) == false)
            {
                throw new MissingMemberException($"Unable to find the property 'streams' within the received probe data");
            }
            if (streamsToken.Type != JTokenType.Array)
            {
                throw new InvalidDataException($"Probe data contained the 'streams' property but it was of the unexpected type '{streamsToken.Type}'");
            }
            if (rootObject.TryGetValue("chapters", StringComparison.InvariantCulture, out var chaptersToken) == false)
            {
                throw new MissingMemberException($"Unable to find the property 'chapters' within the received probe data");
            }
            if (rootObject.TryGetValue("format", StringComparison.InvariantCulture, out var formatToken) == false)
            {
                throw new MissingMemberException($"Unable to find the property 'chapters' within the received probe data");
            }

            // Create the basic video details object that can be filled
            var result = new VideoDetails
            {
                Format = formatToken?.ToObject<VideoFormat>() ?? new VideoFormat(),
                Chapters = chaptersToken?.ToObject<List<VideoChapter>>() ?? new List<VideoChapter>()
            };

            // Individually parse the different streams to their respective objects
            foreach (var streamData in streamsToken.Children())
            {
                // Look for the codec_type property that describes the type of stream
                var codecTypeElement = streamData["codec_type"];
                if (codecTypeElement == null)
                {
                    Logger.Error($"Encountered an error while parsing probe data. Stream was found that didn't include a 'codec_type' property\n{streamData}");
                    continue;
                }

                // Get the type of codec that is in use
                var codecType = codecTypeElement.Value<string>();
                if (string.IsNullOrWhiteSpace(codecType))
                {
                    Logger.Error($"Encountered an error while parsing probe data. Stream was found that had an empty 'codec_type' property\n{streamData}");
                    continue;
                }

                // Determine the type of object that is needed
                Type streamType = codecType switch
                {
                    "video" => typeof(VideoDataStream),
                    "audio" => typeof(AudioDataStream),
                    "subtitle" => typeof(SubtitleDataStream),
                    _ => typeof(UnknownDataStream)
                };

                // Parse the data into the stream object that we can handle
                IDataStream? stream = streamData.ToObject(streamType) as IDataStream;
                if (stream is null)
                {
                    Logger.Error($"Encountered an error while parsing probe data. Determined the stream to be '{streamType.Name}' but was unable to deserialise\n{streamData}");
                    continue;
                }
                result.Streams.Add(stream);
            }
            return result;
        }
    }
}
