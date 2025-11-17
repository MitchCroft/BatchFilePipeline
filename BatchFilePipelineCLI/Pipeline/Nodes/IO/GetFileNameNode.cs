using BatchFilePipelineCLI.DynamicProperties;

namespace BatchFilePipelineCLI.Pipeline.Nodes.IO
{
    /// <summary>
    /// A Node that can be used to retrieve the file name from a specified path
    /// </summary>
    [PipelineNode(nameof(GetFileNameNode), NodeUsage.All)]
    internal sealed class GetFileNameNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need to get the path of the file that is to be processed
        /// </summary>
        private readonly Property _pathProperty = Property.Create
        (
            "FilePath",
            "The full path to the file from which the file name is to be extracted",
            typeof(string),
            example: "Path/To/File/Example.txt"
        );
        private readonly Property _includeExtension = Property.Create
        (
            "IncludeExtension",
            "Flags if the file extension should be included in the resulting file name",
            defaultValue: true
        );

        /// <summary>
        /// Defines the property that will be used as an output of the node for use in later stages
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The string value that contains the formatted DateTime result",
            typeof(string),
            example: "Example.txt"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_pathProperty, _includeExtension];

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
        public ValueTask<NodeOutput> ProcessNodeResultAsync(IDictionary<string, object?> inputs,
                                                            CancellationToken cancellationToken)
        {
            // Process the string format operation
            try
            {
                string filePath = (string)inputs[_pathProperty.Name]!;
                bool includeExtension = (bool)inputs[_includeExtension.Name]!;
                return ValueTask.FromResult(new NodeOutput
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, includeExtension ? Path.GetFileName(filePath) : Path.GetFileNameWithoutExtension(filePath) }
                    }
                ));
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return ValueTask.FromResult(new NodeOutput(ex)); }
        }
    }
}
