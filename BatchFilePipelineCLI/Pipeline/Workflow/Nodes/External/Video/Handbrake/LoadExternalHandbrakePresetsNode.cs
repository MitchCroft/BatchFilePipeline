using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake.Data;
using BatchFilePipelineCLI.PropertyResolver;
using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake
{
    /// <summary>
    /// Handle the loading of external handbrake presets from an export file that can be used while processing
    /// </summary>
    [PipelineNode(nameof(LoadExternalHandbrakePresetsNode), NodeUsage.All)]
    internal sealed class LoadExternalHandbrakePresetsNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need the path on disc where we can retrieve the manifest options from for processing
        /// </summary>
        private readonly Property _manifestPathProperty = Property.Create
        (
            "ManifestPath",
            "The path to the Handbrake preset export file to load external presets from",
            typeof(string)
        );

        /// <summary>
        /// We'll have loaded the manifest data that can be used in later stages of processing
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The collection of available presets that can be used for processing video media",
            typeof(HandbrakePresetManifestRoot)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_manifestPathProperty];

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
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(IReadOnlyDictionary<string, object?> inputs,
                                                                 CancellationToken cancellationToken)
        {
            // Read in the file as JSON and try to deserialise it for use
            try
            {
                // Get the property to be processed
                string manifestPath = (string)inputs[_manifestPathProperty.Name]!;

                // Load the output
                return ValueTask.FromResult(new ExecutionResult
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, JsonConvert.DeserializeObject<HandbrakePresetManifestRoot>(File.ReadAllText(manifestPath)) }
                    }
                ));
            }

            // Anything going wrong is a problem
            catch (Exception ex) { return ValueTask.FromResult(new ExecutionResult(ex)); }
        }
    }
}
