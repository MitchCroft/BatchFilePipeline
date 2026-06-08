using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Comparison;
using System.Globalization;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control
{
    /// <summary>
    /// Handle the comparison of two values to determine a branching path that should be used
    /// </summary>
    [PipelineNode(nameof(ComparisonNode), NodeUsage.All)]
    internal sealed class ComparisonNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// There are a number of different methods that will be used to compare the different values
        /// </summary>
        private readonly Property _leftValueProperty = Property.Create
        (
            "LeftValue",
            "The first value that will be used in the comparison operation, appearing on the left hand of the check",
            typeof(IComparable),
            "Anything that is IComparable including: the primitive types, DateTime, etc."
        );
        private readonly Property _rightValueProperty = Property.Create
        (
            "RightValue",
            "The second value that will be used in the comparison operation, appearing on the right hand of the check",
            typeof(IComparable),
            "Anything that is IComparable including: the primitive types, DateTime, etc."
        );
        private readonly Property _comparisonModeProperty = Property.Create
        (
            "Mode",
            "The method of comparison that will be used when comparing the different values",
            defaultValue: ComparisonMode.Equal
        );

        /// <summary>
        /// There will be two different types of output from this node
        /// </summary>
        private readonly Property _boolOutputProperty = Property.Create
        (
            "BoolResult",
            "Passes out the boolean result of the comparison as well as setting the connection",
            typeof(bool)
        );
        private readonly Property _intOutputProperty = Property.Create
        (
            "IntResult",
            "Passes out the int result of the comparison as well as setting the connection",
            typeof(int)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [ _leftValueProperty, _rightValueProperty, _comparisonModeProperty ];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [ _boolOutputProperty, _intOutputProperty ];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                 CancellationToken cancellationToken)
        {
            // Read in the values that will be used for testing
            IComparable leftValue = context.GetInput<IComparable>(_leftValueProperty);
            IComparable rightValue = context.GetInput<IComparable>(_rightValueProperty);
            ComparisonMode mode = context.GetInput<ComparisonMode>(_comparisonModeProperty);

            // Perform the comparison
            bool boolResult = ComparisonUtility.Compare(leftValue, mode, rightValue, out int intResult);
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _boolOutputProperty.Name, boolResult },
                    { _intOutputProperty.Name, intResult }
                },
                nextNode: boolResult.ToString(CultureInfo.InvariantCulture)
            ));
        }
    }
}
