using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control
{
    /// <summary>
    /// Raise an intentional failure in the process for the given entry
    /// </summary>
    [PipelineNode(nameof(FailNode), NodeUsage.All)]
    internal sealed class FailNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The different properties that can be used when intentionally causing a failure event
        /// </summary>
        private readonly Property _codeProperty = Property.Create
        (
            "Code",
            "The result code that will be used for the failure message",
            -1
        );
        private readonly Property _messageProperty = Property.Create
        (
            "Message",
            "The message that should be used as the failure reason",
            "Fail Node Reached"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_codeProperty, _messageProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => Array.Empty<Property> ();

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                 CancellationToken cancellationToken)
        {
            // Get the property details that can be used for the failure event
            int code = context.GetInput<int>(_codeProperty);
            string message = context.GetInput<string>(_messageProperty);

            // Raise the failure
            return ValueTask.FromResult(new ExecutionResult(code, message));
        }
    }
}
