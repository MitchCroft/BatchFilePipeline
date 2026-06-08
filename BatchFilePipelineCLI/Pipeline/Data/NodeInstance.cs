using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Nodes;

namespace BatchFilePipelineCLI.Pipeline.Data
{
    /// <summary>
    /// Pair a node with its description to be used during execution of the pipeline
    /// </summary>
    public readonly struct NodeInstance
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The instance of the functional node that will be used to perform the work of this node during execution
        /// </summary>
        public readonly INode Node;

        /// <summary>
        /// The description of the node from the pipeline that defines how the information should be inputted and outputted for this node during execution
        /// </summary>
        public readonly NodeDescription Description;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the pair reference that can be used for processing data
        /// </summary>
        public NodeInstance(INode node, NodeDescription description)
        {
            Node = node;
            Description = description;
        }

        /// <summary>
        /// Return the name of this pair from the internal values
        /// </summary>
        /// <returns>Find a value contained that can be used for display</returns>
        public override string ToString() =>
            Description.Name ??
            Node.GetType().Name;

        /// <summary>
        /// Deconstruct the instance into its component parts
        /// </summary>
        public void Deconstruct(out INode node, out NodeDescription description)
        {
            node = Node;
            description = Description;
        }
    }
}
