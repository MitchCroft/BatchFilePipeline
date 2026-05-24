using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.External;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFmpeg
{
    /// <summary>
    /// Manage a process that allows for the stripping of specific streams from a file
    /// </summary>
    [PipelineNode(nameof(RemoveStreamsNode), NodeUsage.Process)]
    internal sealed class RemoveStreamsNode : IPipelineNode
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
        private readonly Property _fileInfoProperty = Property.Create
        (
            "TargetInfo",
            $"The {nameof(VideoDetails)} object that describes the video file that is going to update the subtitle streams",
            typeof(VideoDetails)
        );
        private readonly Property _outputPathProperty = Property.Create
        (
            "OutputPath",
            "The path on disk where the resulting file should be located after being processed",
            typeof(string)
        );

        /// <summary>
        /// We can pass out some simple values for processing
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out the path of the final remuxed file for later use in the graph",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_executableProperty, _sourceProperty, _streamsProperty, _fileInfoProperty, _outputPathProperty];

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
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                       CancellationToken cancellationToken)
        {
            // Get the values that are needed for operation
            string executable = context.GetInput<string>(_executableProperty);
            string source = context.GetInput<string>(_sourceProperty);
            IEnumerable streams = context.GetInput<IEnumerable>(_streamsProperty);
            VideoDetails fileInfo = context.GetInput<VideoDetails>(_fileInfoProperty);
            string outputPath = context.GetInput<string>(_outputPathProperty);

            // We need a list of all the streams that are being kept
            List<IDataStream> streamsToRemove = new List<IDataStream>();
            foreach (var stream in streams)
            {
                if (stream is not IDataStream dataStream)
                {
                    Logger.Error($"[{nameof(RemoveStreamsNode)}] Unable to remove '{stream}' as it is not a stream");
                    continue;
                }
                streamsToRemove.Add(dataStream);
            }

            // We have our list of streams to remove
            streamsToRemove.Sort((l, r) => l.Index.CompareTo(r.Index));
            Logger.Log($"[{nameof(RemoveStreamsNode)}] Identified {streamsToRemove.Count} streams to remove:\n\t{string.Join("\n\t", streamsToRemove)}");

            // Get the list of streams to keep
            var streamsToKeep = fileInfo.Streams
                .Where(x => streamsToRemove.Any(y => y.Index == x.Index) == false);

            // Make sure the output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            // Run the ffmpeg process to strip out the required streams
            var result = await ExternalProcess.RunAsync
            (
                executable,
                $"-i \"{source}\" {string.Join(" ", streamsToKeep.Select(x => $"-map 0:{x.Index}"))} -c copy \"{outputPath}\"",
                onOutput: msg => Logger.Log($"[{nameof(RemoveStreamsNode)}] {msg}"),
                onError: msg => Logger.Log($"[{nameof(RemoveStreamsNode)}] {msg}"),
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

            // Check that we have a video at the output point
            if (File.Exists(source) == false)
            {
                return new ExecutionResult(404, $"[{nameof(RemoveStreamsNode)}] Processed file '{source}' but was unable to generate an output file at '{outputPath}'");
            }

            // We have successfully removed the desired streams
            Logger.Success($"[{nameof(RemoveStreamsNode)}] Successfully removed streams from '{source}'\n\t{string.Join("\n\t", streamsToRemove)}");
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
