using BatchFilePipelineCLI.DynamicProperties;

namespace BatchFilePipelineCLI.Pipeline.Nodes.IO
{
    /// <summary>
    /// Define a Node that can be used to combine multiple path segments into a single path
    /// </summary>
    [PipelineNode(nameof(CombinePathNode), NodeUsage.All)]
    internal sealed class CombinePathNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need to get the different path segments that are to be combined
        /// </summary>
        private readonly Property _firstPathProperty = Property.Create
        (
            "LeftPath",
            "The first segment of the path that is to be combined",
            typeof(string),
            "Parent/Directory/"
        );
        private readonly Property _secondPathProperty = Property.Create
        (
            "RightPath",
            "The second segment of the path that is to be combined",
            typeof(string),
            "Child/Directory/File.txt"
        );

        /// <summary>
        /// We will produce the single combined path as an output
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The resulting combined path from the provided segments",
            typeof(string),
            "Parent/Directory/Child/Directory/File.txt"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_firstPathProperty, _secondPathProperty];

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
                string firstPath = (string)inputs[_firstPathProperty.Name]!;
                string secondPath = (string)inputs[_secondPathProperty.Name]!;
                return ValueTask.FromResult(new NodeOutput
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, Path.Combine(firstPath, secondPath) }
                    }
                ));
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return ValueTask.FromResult(new NodeOutput(ex)); }
        }
    }
}
