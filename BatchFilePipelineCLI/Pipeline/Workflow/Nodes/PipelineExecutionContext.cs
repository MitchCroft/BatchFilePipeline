using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;
using System.Runtime.CompilerServices;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes
{
    /// <summary>
    /// Provide a collection of information that can be used when processing the execution of a Pipeline Node
    /// </summary>
    public readonly struct PipelineExecutionContext
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The environment variables that are currently being used for processing the execution of the graph
        /// </summary>
        public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; init; }

        /// <summary>
        /// The runtime variables that are currently being used for processing the execution of the graph
        /// </summary>
        public IReadOnlyDictionary<string, object?> RuntimeVariables { get; init; }

        /// <summary>
        /// The input variables that have been identified for use within the current Node being processed
        /// </summary>
        public IReadOnlyDictionary<string, object?> InputVariables { get; init; }

        /// <summary>
        /// Callback function that can be used to retrieve a sub-process graph with a specified ID
        /// </summary>
        public Func<string, GraphRunner?>? GetSubProcess { get; init; }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the input variable for the specified property
        /// </summary>
        /// <typeparam name="T">The type of the value that is expected for the property</typeparam>
        /// <param name="property">The property that is to be retrieved for processing</param>
        /// <returns>Returns the value that is stored in the inputs</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetInput<T>(Property property) => (T)InputVariables[property.Name]!;
    }
}
