using BatchFilePipelineCLI.Utility.Preserve;

namespace BatchFilePipelineCLI.Pipeline.Nodes
{
    /// <summary>
    /// Mark a <see cref="IPipelineNode"/> implementing object with the information required for construction
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PipelineNodeAttribute : PreserveAttribute
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// A unique identifier for the type of the attached node that can be used to identify the required type in a workflow description
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Flags the location where the attached node is able to be used in a workflow
        /// </summary>
        public NodeUsage UsageFlags { get; private set; }

        /// <summary>
        /// Flags if the instance of the node should be created fresh for each instance in a workflow, or if it can be shared between different uses
        /// </summary>
        public bool IsShared { get; set; } = true;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the node attribute with the information required to process the usage within a pipeline
        /// </summary>
        /// <param name="id">A unique identifier for the type of the attached node that can be used to identify the required type in a workflow description</param>
        /// <param name="usageFlags">Flags the location where the attached node is able to be used in a workflow</param>
        /// <exception cref="ArgumentNullException">Thrown if the type value is not specified with a real value</exception>
        public PipelineNodeAttribute(string id, NodeUsage usageFlags)
        {
            if (string.IsNullOrWhiteSpace(id) == true)
            {
                throw new ArgumentNullException(nameof(id));
            }
            ID = id;
            UsageFlags = usageFlags;
        }
    }
}
