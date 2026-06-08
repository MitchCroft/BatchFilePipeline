using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Manifest
{
    /// <summary>
    /// Node that will be used to get data values for entries in the manifest
    /// </summary>
    [Node]
    public sealed class GetManifestDataNode : ManifestNodeBase
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There will need to be a set of properties that can be used to retrieve the manifest data
        /// </summary>
        private readonly Property _identifierProperty = Property.Create
        (
            "Identifier",
            "The name of the storage container that the element should be retrieved",
            typeof(string),
            "Path/To/File.txt"
        );
        private readonly Property _keyProperty = Property.Create
        (
            "Key",
            "The key for the property that is to be retrieved",
            typeof(string),
            "State"
        );
        private readonly Property _defaultValueProperty = Property.Create
        (
            "DefaultValue",
            "A default value that will be returned if there is no existing value",
            defaultValue: string.Empty
        );

        /// <summary>
        /// We're going to be passing out a single value for use later in the workflow
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out the value that was contained in the container, or the default value if none",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public override IList<Property> GetInputProperties() => [_manifestPathProperty, _identifierProperty, _keyProperty, _defaultValueProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public override IList<Property> GetOutputProperties() => [ _outputProperty ];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public override ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            // Get the values that will be needed
            string manifestPath = context.GetInput<string>(_manifestPathProperty);
            string identifier = context.GetInput<string>(_identifierProperty);
            string key = context.GetInput<string>(_keyProperty);
            string defaultValue = context.GetInput<string>(_defaultValueProperty);

            // Read the manifest data
            var manifest = ReadManifestData(manifestPath);
            if (manifest.TryGetValue(identifier, out var metaData) == false ||
                metaData.TryGetValue(key, out var value) == false)
            {
                value = defaultValue;
            }
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, value }
                }
            ));
        }
    }
}
