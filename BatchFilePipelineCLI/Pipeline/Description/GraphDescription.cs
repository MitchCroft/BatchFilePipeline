using System.Xml.Serialization;

namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// Information about a graph of nodes that can be processed during a workflow
    /// </summary>
    public sealed class GraphDescription : IWorkflowElement
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The human-readable name of the graph that is being used
        /// </summary>
        /// <remarks>
        /// This is intended to be able to distinguish the different graphs defined in a workflow
        /// </remarks>
        [XmlAttribute(nameof(Name))]
        public string? Name { get; set; } = null;

        /// <summary>
        /// The unique ID of the graph
        /// </summary>
        /// <remarks>
        /// This is intended to give a connection to be used in the sub-process setup
        /// </remarks>
        [XmlAttribute(nameof(Id))]
        public string? Id { get; set; } = null;

        /// <summary>
        /// The unique ID of the node on the graph that will be run as the entry point for the graph when it is processed
        /// </summary>
        [XmlAttribute(nameof(EntryNodeId))]
        public string? EntryNodeId { get; set; } = null;

        /// <summary>
        /// An array of the required properties that must exist at runtime for the graph to be able to run
        /// </summary>
        [XmlElement("Required")]
        public string[] RequiredProperties { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The collection of node definitions that make up this graph
        /// </summary>
        [XmlElement("Node")]
        public NodeDescription[] Nodes { get; set; } = Array.Empty<NodeDescription>();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Format the name and ID of this graph for output display
        /// </summary>
        public override string ToString() => $"{Name} ({Id})";
    }
}
