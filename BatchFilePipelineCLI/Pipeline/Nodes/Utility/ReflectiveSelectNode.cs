using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Utility
{
    /// <summary>
    /// Provide a utility function for selecting a sub-value of an object based on reflection
    /// </summary>
    [Node]
    public sealed class ReflectiveSelectNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need several values to know how to process this Node operation
        /// </summary>
        private readonly Property _sourceProperty = Property.Create
        (
            "Source",
            "The base object that will be processed to identify the sub-value for use",
            typeof(object)
        );
        private readonly Property _pathProperty = Property.Create
        (
            "Path",
            "The path that you would follow on the source object to reach the value for use, seperated by '.' for the different fields/properties",
            typeof(string),
            "Property.Field"
        );

        /// <summary>
        /// This node will pass out whatever object was identified for use
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The resulting object value that was selected from the operation",
            typeof(object)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_sourceProperty, _pathProperty];

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
            // Retrieve the properties that will be processed
            object sourceProperty = context.GetInput<object>(_sourceProperty);
            string pathProperty = context.GetInput<string>(_pathProperty);

            // Process the selection of the required sub-value for use
            if (Resolver.TryResolveReflectiveProperty(sourceProperty, pathProperty, out var resultObj) == false)
            {
                Logger.Warning($"[{nameof(ReflectiveSelectNode)}] Encountered a null value while processing the path '{pathProperty}' for the object '{sourceProperty}'");
            }

            // Find the collection of files that need to be handled
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, resultObj }
                }
            ));
        }
    }
}
