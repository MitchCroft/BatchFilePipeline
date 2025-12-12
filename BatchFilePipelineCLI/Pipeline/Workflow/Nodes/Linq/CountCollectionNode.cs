using BatchFilePipelineCLI.PropertyResolver;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Linq
{
    /// <summary>
    /// Count all of the elements that are contained within a collection for processing
    /// </summary>
    [PipelineNode(nameof(CountCollectionNode), NodeUsage.All)]
    internal sealed class CountCollectionNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need a number of different inputs to know how to process the data that is being passed in
        /// </summary>
        private readonly Property _collectionProperty = Property.Create
        (
            "Collection",
            "The collection of values that are to be processed as a part of the comparison",
            typeof(IEnumerable)
        );

        /// <summary>
        /// Passes out the number of elements that are contained in the collection
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The number of elements that are contained within the collection",
            typeof(int)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_collectionProperty];

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
            // Retrieve the values that will be used for processing
            IEnumerable collection = (IEnumerable)inputs[_collectionProperty.Name]!;

            // Count the elements that are used in the collection
            int count = 0;
            foreach (var _ in collection)
            {
                ++count;
            }

            // Create the output result that will be used
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, count }
                }
            ));
        }
    }
}
