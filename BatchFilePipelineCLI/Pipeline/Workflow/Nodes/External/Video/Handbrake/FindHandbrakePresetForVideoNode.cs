using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake.Data;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake
{
    /// <summary>
    /// Find a <see cref="HandbrakePresetOption"/> that would be the best fit for a provided video file
    /// </summary>
    [PipelineNode(nameof(FindHandbrakePresetForVideoNode), NodeUsage.Process)]
    internal sealed class FindHandbrakePresetForVideoNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Collect the different values that can eb used to determine the result of the operation
        /// </summary>
        private readonly Property _videoInfoProperty = Property.Create
        (
            "VideoInfo",
            $"The {nameof(VideoDetails)} object that represents the information of the video file being processed",
            typeof(VideoDetails)
        );
        private readonly Property _handbrakePresetsProperty = Property.Create
        (
            "HandbrakePresets",
            $"The collection of {nameof(HandbrakePresetOption)} elements that will be used to identify the best for the video file",
            typeof(IEnumerable<HandbrakePresetOption>)
        );

        /// <summary>
        /// Find the preset option that will be best suited for working with the file from the available selection
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The handbrake preset option that will be best suited for working with the specified video",
            typeof(HandbrakePresetOption)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_videoInfoProperty, _handbrakePresetsProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                 CancellationToken cancellationToken)
        {
            // Get the properties that will be used for processing
            VideoDetails videoInfo = context.GetInput<VideoDetails>(_videoInfoProperty);
            IEnumerable<HandbrakePresetOption> handbrakePresets = context.GetInput<IEnumerable<HandbrakePresetOption>>(_handbrakePresetsProperty);

            // Look for the video stream in the file that can be compared against
            VideoDataStream? videoStream = videoInfo.Streams.FirstOrDefault(x => x.StreamType == StreamType.Video) as VideoDataStream;
            if (videoStream == null)
            {
                throw new NullReferenceException($"[{nameof(FindHandbrakePresetForVideoNode)}] Unable to find a video stream within the supplied video info '{videoInfo}'");
            }

            // Find the preset that will be best suited for the video file
            HandbrakePresetOption? bestOption = null;
            int bestDifference = int.MaxValue;
            foreach (var preset in handbrakePresets)
            {
                // If the preset is invalid, don't bother
                if (preset.IsValid == false)
                {
                    continue;
                }

                // Check the area that it covers
                int presetArea = preset.PictureWidth * preset.PictureHeight;
                int difference = Math.Abs(presetArea - videoStream.Area);
                if (difference > bestDifference)
                {
                    continue;
                }
                bestOption = preset;
                bestDifference = difference;
            }

            // If we couldn't find an option, that's a problem
            if (bestOption == null)
            {
                throw new NullReferenceException($"[{nameof(FindHandbrakePresetForVideoNode)}] Unable to find a {nameof(HandbrakePresetOption)} within the collection for use");
            }

            // Return the result for use
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, bestOption }
                }
            ));
        }
    }
}
