using BatchFilePipelineCLI.Pipeline.Description;

namespace BatchFilePipelineCLI.Pipeline.Data
{
    /// <summary>
    /// Defines a collection of <see cref="IPipelineNode"/> elements that can be actioned to perform a specific set of work
    /// </summary>
    public sealed class Graph
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The source element that was used to construct this graph
        /// </summary>
        private readonly IWorkflowElement _source;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The name that has been given to this graph for display and logging
        /// </summary>
        public string Name => _source.Name ?? nameof(Graph);

        /// <summary>
        /// The unique id that has been given to this graph for reference and linking to other graphs
        /// </summary>
        public string Id => _source.Id!;

        /// <summary>
        /// The id of the node within this graph that will be used as the entry point for processing the graph
        /// </summary>
        public string EntryNodeId { get; private set; }

        /// <summary>
        /// A list of all the properties that this graph is expecting to be available at runtime to be able to process successfully
        /// </summary>
        public IReadOnlyList<string> RequiredProperties { get; private set; }

        /// <summary>
        /// The collection of nodes that exist in this graph that can be used to process the actual work
        /// </summary>
        public IReadOnlyDictionary<string, NodeInstance> Nodes { get; private set; }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the graph with the collection of data that will be used for processing the work
        /// </summary>
        public Graph(IWorkflowElement source,
                     string entryNodeId,
                     IReadOnlyList<string> requiredProperties,
                     IReadOnlyDictionary<string, NodeInstance> nodes)
        {
            _source = source;
            EntryNodeId = entryNodeId;
            RequiredProperties = requiredProperties;
            Nodes = nodes;
        }

        /// <summary>
        /// Get the string representation of this graph, which will be the name or id of the source element if available, otherwise it will be the name of this class
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Name} ({Id})";
    }
}
