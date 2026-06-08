using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Manifest
{
    /// <summary>
    /// Node that will be used to set data values for entries in the manifest
    /// </summary>
    [PipelineNode(nameof(SetManifestDataNode), NodeUsage.Process)]
    internal sealed class SetManifestDataNode : ManifestNodeBase
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There will need to be a set of properties that can be used to adjust the manifest data
        /// </summary>
        private readonly Property _identifierProperty = Property.Create
        (
            "Identifier",
            "The name of the storage container that the element should be adjusted",
            typeof(string),
            "Path/To/File.txt"
        );
        private readonly Property _keyProperty = Property.Create
        (
            "Key",
            "The key for the property that is to be set",
            typeof(string),
            "State"
        );
        private readonly Property _valueProperty = Property.Create
        (
            "Value",
            "The new value that should be stored within the manifest under the key",
            typeof(string),
            "Completed"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public override IList<Property> GetInputProperties() => [ _manifestPathProperty, _identifierProperty, _keyProperty, _valueProperty ];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public override IList<Property> GetOutputProperties() => Array.Empty<Property>();

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public override ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                          CancellationToken cancellationToken)
        {
            // Get the values that will be needed
            string manifestPath = context.GetInput<string>(_manifestPathProperty);
            string identifier = context.GetInput<string>(_identifierProperty);
            string key = context.GetInput<string>(_keyProperty);
            string value = context.GetInput<string>(_valueProperty);

            // Update the manifest data
            var manifest = ReadManifestData(manifestPath);
            if (manifest.Data.TryGetValue(identifier, out var metaData) == false)
            {
                metaData =
                manifest.Data[identifier] = new Dictionary<string, string>(1);
            }
            metaData[key] = value;
            WriteManifestData(manifestPath, manifest);
            return ValueTask.FromResult(new ExecutionResult());
        }
    }
}
