namespace BatchFilePipelineCLI.Pipeline.Data
{
    /// <summary>
    /// Contain all of the graph elements that can be used to process the required work for a pipeline
    /// </summary>
    public sealed class Workflow
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The id of the graph within this workflow that should be considered the default entry point for a running workflow
        /// </summary>
        public string? EntryGraphId { get; private set; }

        /// <summary>
        /// Flags if this workflow is a library of graphs that can be run
        /// </summary>
        /// <remarks>
        /// A library is a collection of functionality, but it can't be run directly as a workflow.
        /// It must be referenced by another executing workflow that specifies an entry graph.
        /// </remarks>
        public bool IsLibrary => string.IsNullOrWhiteSpace(EntryGraphId);

        /// <summary>
        /// The collection of graph definitions that are contained within the workflow that can be used for processing
        /// </summary>
        public IReadOnlyDictionary<string, Graph> Graphs { get; private set; }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the workflow with the defined values that can be processed
        /// </summary>
        /// <param name="entryGraphId">The id of the contained graph that will be used as the entry point of the workflow</param>
        /// <param name="graphs">The lookup collection of the graphs that can be used for processing work</param>
        public Workflow(string? entryGraphId,
                        IReadOnlyDictionary<string, Graph> graphs)
        {
            EntryGraphId = entryGraphId;
            Graphs = graphs;
        }
    }
}
