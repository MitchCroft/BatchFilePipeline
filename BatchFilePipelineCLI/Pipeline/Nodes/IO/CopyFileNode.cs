using BatchFilePipelineCLI.DynamicProperties;

namespace BatchFilePipelineCLI.Pipeline.Nodes.IO
{
    /// <summary>
    /// Define a Node that can be used to copy a file from one location to another
    /// </summary>
    [PipelineNode(nameof(CopyFileNode), NodeUsage.All)]
    internal sealed class CopyFileNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There will be a number of properties that will be used in this operation
        /// </summary>
        private readonly Property _sourceProperty = new Property
        (
            "SourceFileName",
            "The path to the original file that is to be copied",
            typeof(string),
            required: true,
            example: "Path/To/File/Source.txt"
        );
        private readonly Property _destinationProperty = new Property
        (
            "DestinationFileName",
            "The file path where the copy of the file should be placed",
            typeof(string),
            required: true,
            example: "Path/To/File/Destination.txt"
        );
        private readonly Property _overwriteProperty = new Property
        (
            "Overwrite",
            "Flags if the file should overwrite an existing file at the target location",
            typeof(bool),
            required: false,
            defaultValue: false
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_sourceProperty, _destinationProperty, _overwriteProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => Array.Empty<Property>();

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<NodeOutput> ProcessNodeResultAsync(IDictionary<string, object?> inputs,
                                                            CancellationToken cancellationToken)
        {
            // Process the copy operation
            try
            {
                File.Copy
                (
                    (string)inputs[_sourceProperty.Name]!,
                    (string)inputs[_destinationProperty.Name]!,
                    (bool)inputs[_overwriteProperty.Name]!
                );
                return ValueTask.FromResult(new NodeOutput
                (
                    new Dictionary<string, object?>()
                ));
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return ValueTask.FromResult(new NodeOutput(ex)); }
        }
    }
}
