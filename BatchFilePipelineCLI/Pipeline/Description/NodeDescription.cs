using System.Xml.Serialization;

namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// Describe a Node that can be used within the processing workflow and how it should operate
    /// </summary>
    public sealed class NodeDescription : IWorkflowElement
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The human-readable name of the node stage that is being used
        /// </summary>
        /// <remarks>
        /// This is intended to be able to distinguish the different stages defined in a workflow
        /// </remarks>
        [XmlAttribute(nameof(Name))]
        public string? Name { get; set; } = null;

        /// <summary>
        /// The unique ID of the node stage
        /// </summary>
        /// <remarks>
        /// This is intended to give a connection to be used in the graph traversal setup
        /// </remarks>
        [XmlAttribute(nameof(Id))]
        public string? Id { get; set; } = null;

        /// <summary>
        /// The unique ID of the type of node that is to be created for this stage
        /// </summary>
        /// <remarks>
        /// Each type of node that can be used will have a unique ID that can be used to create specifics
        /// </remarks>
        [XmlAttribute(nameof(TypeId))]
        public string? TypeId { get; set; } = null;

        /// <summary>
        /// The collection of input values that are to be retrieved for the node to process
        /// </summary>
        [XmlElement(nameof(Inputs))]
        public KeyValueSection Inputs { get; set; } = new();

        /// <summary>
        /// The collection of output values that can be mapped for use in the later stages of processing
        /// </summary>
        [XmlElement(nameof(Outputs))]
        public KeyValueSection Outputs { get; set; } = new();

        /// <summary>
        /// The collection of connections that can be used as the next stages for processing
        /// </summary>
        [XmlElement(nameof(Connections))]
        public KeyValueSection Connections { get; set; } = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Format the name and ID of this node for output display
        /// </summary>
        public override string ToString() => $"{Name} ({Id})";
    }
}
