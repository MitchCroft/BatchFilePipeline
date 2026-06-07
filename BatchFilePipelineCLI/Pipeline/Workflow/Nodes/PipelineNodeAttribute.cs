using BatchFilePipelineCLI.Utility.Preserve;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes
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
        /// Flags the location where the attached node is able to be used in a workflow
        /// </summary>
        public NodeUsage UsageFlags { get; set; } = NodeUsage.All;

        /// <summary>
        /// Flags if the instance of the node should be created fresh for each instance in a workflow, or if it can be shared between different uses
        /// </summary>
        public bool IsShared { get; set; } = true;
    }
}
