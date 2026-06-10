using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using System.Globalization;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Time
{
    /// <summary>
    /// Define a Node that can be used to format a DateTime value into a specified string format
    /// </summary>
    [Node]
    public sealed class FormatDateTimeNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The format string that will be used when processing the DateTime value
        /// </summary>
        private readonly Property _formatStringProperty = Property.Create
        (
            "FormatString",
            "The format string that will be used to format the DateTime value for use",
            "yyyy-MM-dd HH:mm:ss",
            example: "Custom DateTime format string, e.g. yyyy-MM-dd HH:mm:ss"
        );

        /// <summary>
        /// The DateTime property that is to be used within the formatting operation
        /// </summary>
        private readonly Property _dateTimeProperty = Property.Create
        (
            "DateTime",
            "The DateTime value that is to be formatted",
            () => DateTime.Now,
            example: "Runtime DateTime value or as a string 11/17/2025 13:52:32"
        );

        /// <summary>
        /// Defines the property that will be used as an output of the node for use in later stages
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The string value that contains the formatted DateTime result",
            typeof(string),
            example: "2025-11-17 13:52:32"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_formatStringProperty, _dateTimeProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            DateTime dateTime = context.GetInput<DateTime>(_dateTimeProperty);
            string formatString = context.GetInput<string>(_formatStringProperty);
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, dateTime.ToString(formatString, CultureInfo.InvariantCulture) }
                }
            ));
        }
    }
}
