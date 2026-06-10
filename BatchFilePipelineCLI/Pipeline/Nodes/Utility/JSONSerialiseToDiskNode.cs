using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Utility
{
    /// <summary>
    /// Save a supplied value of any type to disk as a JSON output result
    /// </summary>
    [Node]
    public sealed class JSONSerialiseToDiskNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need some basic values to know how to process this data
        /// </summary>
        private readonly Property _valueProperty = Property.Create
        (
            "Value",
            "The value that is to be serialised and written to disk",
            typeof(object)
        );
        private readonly Property _outputPathProperty = Property.Create
        (
            "OutputPath",
            "The location on disk where the serialised data should be stored",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_valueProperty, _outputPathProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputPathProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            // Get the values that are needed for processing
            object value = context.GetInput<object>(_valueProperty);
            string outputPath = context.GetInput<string>(_outputPathProperty);

            // Serialise the data to disk
            string json = JsonConvert.SerializeObject(value);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, json);

            // We're good to go
            return ValueTask.FromResult(new ExecutionResult());
        }
    }
}
