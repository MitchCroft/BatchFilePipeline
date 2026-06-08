using BatchFilePipelineCLI.Pipeline.Data;
using BatchFilePipelineCLI.PropertyResolver;
using System.Runtime.CompilerServices;

namespace BatchFilePipelineCLI.Pipeline.Runners
{
    /// <summary>
    /// Contains the current runtime context for the executing element that will be handled
    /// </summary>
    public readonly struct PipelineContext
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The pipeline runner that is being used to execute the running of the work
        /// </summary>
        public IPipelineRunner Runner { get; init; }

        /// <summary>
        /// The id for the current pipeline that is being executed
        /// </summary>
        public PipelineId CurrentPipeline { get; init; }

        /// <summary>
        /// The environment variables that are currently being used for processing the execution of the graph
        /// </summary>
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; }

        /// <summary>
        /// The runtime variables that are currently being used for processing the execution of the graph
        /// </summary>
        public IReadOnlyDictionary<string, object?> RuntimeVariables { get; init; }

        /// <summary>
        /// The input variables that have been identified for use within the current Node being processed
        /// </summary>
        public IReadOnlyDictionary<string, object?> InputVariables { get; init; }

        /// <summary>
        /// The cancellation token that can be used to control the lifespan of the operation
        /// </summary>
        public CancellationToken CancellationToken { get; init; }

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
