using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using BatchFilePipelineCLI.PropertyResolver;
using Newtonsoft.Json;
using System.Text;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Utility
{
    /// <summary>
    /// Allow for the cleansing of a string to substitute a collection of characters in a string for others
    /// </summary>
    [Node]
    public sealed class SubstitutionNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The collection of properties that will be needed to perform the operation
        /// </summary>
        private readonly Property _stringProperty = Property.Create
        (
            "String",
            "The text string that is to be processed to substitute the specified character sequences",
            typeof(string)
        );
        private readonly Property _dictionaryPathProperty = Property.Create
        (
            "DictionaryPath",
            "The path to the JSON file that describes a dictionary of characters that should be replaced when processing",
            typeof(string),
            "Path/To/Lookup.json"
        );

        /// <summary>
        /// Passes out the string input with the required substitutions being made
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out the string input with the required characters updated",
            typeof(string)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_stringProperty, _dictionaryPathProperty];

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
            StringBuilder stringProperty = new(context.GetInput<string>(_stringProperty));
            string dictionaryPathProperty = context.GetInput<string>(_dictionaryPathProperty);

            // Try to read the substitutions data
            string json = File.ReadAllText(dictionaryPathProperty);
            Dictionary<string, string>? substitutions = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            // If there are substitutions that can be made
            if (substitutions is not null &&
                substitutions.Count > 0)
            {
                foreach (var (find, replace) in substitutions)
                {
                    stringProperty.Replace(find, replace);
                }
            }

            // We have the final string that is needed
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, stringProperty.ToString() }
                }
            ));
        }
    }
}
