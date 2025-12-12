using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake.Data;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility;
using BatchFilePipelineCLI.Utility.External;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake
{
    /// <summary>
    /// Handle the process of transcoding a video file with a specific preset
    /// </summary>
    [PipelineNode(nameof(TranscodeVideoNode), NodeUsage.Process)]
    internal sealed class TranscodeVideoNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need to know some settings to be able to process the 
        /// </summary>
        private readonly Property _executableProperty = Property.Create
        (
            "Handbrake",
            "The path to the Handbrake CLI executable that can be used to perform the transcode operation",
            typeof(string),
            "Path/To/HandbrakeCLI.exe"
        );
        private readonly Property _videoPathProperty = Property.Create
        (
            "VideoPath",
            "The path to the video file that is to be processed by this transcode operation",
            typeof(string),
            "Path/To/Video.mp4"
        );
        private readonly Property _presetProperty = Property.Create
        (
            "Preset",
            $"The {nameof(HandbrakePresetOption)} that will be used to process the transcode operation for the video file",
            typeof(HandbrakePresetOption)
        );
        private readonly Property _outputDirProperty = Property.Create
        (
            "OutputDir",
            "The directory where the final file should be located. The file will share the name of the input and the extension will depend on the preset file format",
            typeof(string),
            "Path/To/Output/Directory/"
        );
        private readonly Property _allowHardwareAccelerationProperty = Property.Create
        (
            "AllowHardwareAcceleration",
            "Flags if hardware acceleration encoding should be used if it's available on the computer and the selected codec",
            false
        );
        private readonly Property _additionalProcessArgsProperty = Property.Create
        (
            "AdditionalProcessArgs",
            "Any additional command line arguments that should be passed to the Handbrake CLI process when executing the transcode operation",
            "--markers",
            "E.g. --preset-import-file Path/To/CustomPreset.json"
        );

        /// <summary>
        /// We can export the final output file path so it can be used in later stages
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The file path to the output video file that was generated from the transcode operation, with the extension as defined by the preset",
            typeof(string)
        );

        /// <summary>
        /// Cache a lookup collection of the different hardware encoders that can be used for processing operations based on the codec type
        /// </summary>
        private readonly Dictionary<string/*Encoder*/, string[]/*Hardware Encoder Names*/> _hardwareEncoderLookup = new Dictionary<string, string[]>
        {
            { "x264",       [ "nvenc_h264", "qsv_h264", "vce_h264", "vt_h264" ] },
            { "x265",       [ "nvenc_h265", "qsv_h265", "vce_h265", "vt_h265" ] },
            { "x265_10bit", [ "nvenc_h265", "qsv_h265", "vce_h265", "vt_h265" ] },
            { "x265_12bit", [ "nvenc_h265", "qsv_h265", "vce_h265", "vt_h265" ] },
        };

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_executableProperty, _videoPathProperty, _presetProperty, _outputDirProperty, _allowHardwareAccelerationProperty, _additionalProcessArgsProperty];

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
            // Get the input values that will be used to process the transcode operation
            string executable = (string)inputs[_executableProperty.Name]!;
            string videoPath = (string)inputs[_videoPathProperty.Name]!;
            HandbrakePresetOption preset = (HandbrakePresetOption)inputs[_presetProperty.Name]!;
            string outputDir = (string)inputs[_outputDirProperty.Name]!;
            bool allowHardwareAcceleration = (bool)inputs[_allowHardwareAccelerationProperty.Name]!;
            string additionalProcessArgs = (string)inputs[_additionalProcessArgsProperty.Name]!;

            // Find the extension that will be used for the output file
            string extension = preset.FileFormat switch
            {
                "av_mp4" => ".mp4",
                "av_mkv" => ".mkv",
                "av_webm" => ".webm",
                _ => throw new InvalidOperationException($"Unsupported file format specified in preset '{preset.FileFormat}'")
            };

            // Work out the full output path that will be used for the transcode operation
            string outputPath = Path.Combine
            (
                outputDir,
                $"{Path.GetFileNameWithoutExtension(videoPath)}{extension}"
            );
            Directory.CreateDirectory(outputDir);

            // Assemble the arguements that will be used to process the transcode operation
            StringBuilder arguments = new StringBuilder($"-i \"{videoPath}\" -o \"{outputPath}\" --preset \"{preset.PresetName}\" {additionalProcessArgs}");

            // Check if we are trying to use hardware acceleration for processing
            if (allowHardwareAcceleration == true)
            {
                // Retrieve the hardware acecleration encoder that can be used
                string? hardwareAccelerator = await TryFindSupportedHardwareEncoderAsync(executable, preset, cancellationToken);
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return new ExecutionResult();
                }

                // If we have an encoder option, we can apply it to the arguments
                if (string.IsNullOrWhiteSpace(hardwareAccelerator) == false)
                {
                    arguments.Append($" --encoder {hardwareAccelerator}");
                }
            }

            // Handbrake formats their own ETA messages, we can just handle the output logs
            int counter = 0;
            double prevPercentage = 0;
            Action<string> logHandler = msg =>
            {
                // If there isn't an "ETA" in the message, log it like normal
                if (msg.Contains(" ETA ") == false)
                {
                    Logger.Log($"[{nameof(TranscodeVideoNode)}] {msg}");
                    return;
                }

                // Limit the number of progress logs that are generated, these can be very frequent
                ++counter;
                if (counter % 25 != 0)
                {
                    return;
                }

                // Output the basic log message for handling
                Logger.Log($"[{nameof(TranscodeVideoNode)}] {msg}");

                // Try to find the percentage value in the log line
                if (CompletionEstimateUtility.TryParsePercentage(msg, out var percentage) == false)
                {
                    return;
                }

                // If we pass a milestone, then we can log that as well
                int prevBucket = (int)Math.Floor(prevPercentage / 25);
                int curBucket = (int)Math.Floor(percentage / 25);
                prevPercentage = percentage;
                if (curBucket > prevBucket)
                {
                    Logger.Success($"[{nameof(TranscodeVideoNode)}] {msg}");
                }
            };

            // We can start the external process, we want to be able to format and display progression estimates
            var result = await ExternalProcess.RunAsync
            (
                executable,
                arguments.ToString(),
                onOutput: logHandler,
                onError: logHandler,
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
            if (File.Exists(outputPath) == false)
            {
                return new ExecutionResult(404, $"[{nameof(TranscodeVideoNode)}] Processed video '{videoPath}' but was unable to generate an output file at '{outputPath}'");
            }

            // We have successfully transcoded the file
            Logger.Success($"[{nameof(TranscodeVideoNode)}] Successfully transcoded '{videoPath}' using preset '{preset}' to '{outputPath}'");
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, outputPath }
                }
            );
        }

        //PRIVATE

        /// <summary>
        /// Try to identify a hardware encoder available for use with the specified file to be processed
        /// </summary>
        /// <param name="handbrakeExecutable">The executable path for the Handbrake CLI that can be run</param>
        /// <param name="preset">The preset that is being applied to the transcoding process</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the name of the encoder that can be used, or null if none could be found</returns>
        private async ValueTask<string?> TryFindSupportedHardwareEncoderAsync(string handbrakeExecutable,
                                                                              HandbrakePresetOption preset,
                                                                              CancellationToken cancellationToken)
        {
            // If there is no video encoder, then we can't do anything
            if (string.IsNullOrWhiteSpace(preset.VideoEncoder) == true)
            {
                Logger.Log($"[{nameof(TranscodeVideoNode)}] No video encoder defined for the preset {preset}");
                return null;
            }

            // If there are no hardware acceleration options for this codec, nothing we can do
            if (_hardwareEncoderLookup.TryGetValue(preset.VideoEncoder, out var hardwareOptions) == false ||
                hardwareOptions == null)
            {
                Logger.Log($"[{nameof(TranscodeVideoNode)}] Video codec '{preset.VideoEncoder}' has no hardware encoders available");
                return null;
            }

            // Run the help process to get a list of what Handbrake supports in this environment
            var result = await ExternalProcess.RunAsync(handbrakeExecutable, "--help", cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested == true)
            {
                return null;
            }

            // If there was an error
            if (result.DidError == true)
            {
                Logger.Error($"[{nameof(TranscodeVideoNode)}] Failed to retrieve Handbrake CLI options: {result}");
                return null;
            }
            Logger.Log($"[{nameof(TranscodeVideoNode)}] HandbrakeCLI help resulted in: {result}");

            // Regex to find the list of encoders after "-e, --encoder <string>"
            // The output typically looks like: "Select video encoder: x264 nvenc_h264 ..."
            var match = Regex.Match(result.StdOut + result.StdErr, @"-e, --encoder <string>\s+Select video encoder:(.*?)(?=\n\s*-)", RegexOptions.Singleline);
            if (match.Success == false)
            {
                Logger.Warning($"[{nameof(TranscodeVideoNode)}] Unable to find any hardware encoding options in the result");
                return null;
            }

            // Get the set of available encoders that can be used
            var available = match.Groups[1].Value
                .Split([' ', '\r', '\n', '\t', '|'], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToHashSet();
            Logger.Log($"[{nameof(TranscodeVideoNode)}] Identified {available.Count} supported hardware encoders:\n\t{string.Join("\n\t", available)}");

            // Check to see if any of our hardware options are in the list
            for (int i = 0; i < hardwareOptions.Length; ++i)
            {
                if (available.Contains(hardwareOptions[i]))
                {
                    return hardwareOptions[i];
                }
            }
            Logger.Log($"[{nameof(TranscodeVideoNode)}] Unable to find any supported hardware encoders for processing transcode operation");
            return null;
        }
    }
}
