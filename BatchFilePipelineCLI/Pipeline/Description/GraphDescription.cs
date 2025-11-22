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
