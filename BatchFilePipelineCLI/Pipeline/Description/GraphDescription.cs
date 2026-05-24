using System.Xml.Serialization;

namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// Information about a graph of nodes that can be processed during a workflow
    /// </summary>
    public sealed class GraphDescription : IGraphDescription
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The human-readable name of the graph that is being used
        /// </summary>
        /// <remarks>
        /// This is intended to be able to distinguish the different graphs defined in a workflow
        /// </remarks>
        [XmlAttribute("Name")]
        public string? Name { get; set; } = null;

        /// <summary>
        /// The unique ID of the graph
        /// </summary>
        /// <remarks>
        /// This is intended to give a connection to be used in the sub-process setup
        /// </remarks>
        [XmlAttribute("ID")]
        public string? ID { get; set; } = null;

        /// <summary>
        /// Define an additional layer of pipeline environment properties that can be applied to the process
        /// </summary>
        [XmlElement("Environment")]
        public KeyValueSection Environment { get; set; } = new();

        /// <summary>
        /// The collection of node definitions that make up this graph
        /// </summary>
        [XmlElement("Node")]
        public NodeDescription[] Nodes { get; set; } = Array.Empty<NodeDescription>();
    }
}
