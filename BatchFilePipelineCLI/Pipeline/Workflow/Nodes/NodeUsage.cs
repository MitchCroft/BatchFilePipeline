namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes
{
    /// <summary>
    /// Define flags for where in the workflow the node can be used for processing
    /// </summary>
    [Flags]
    public enum NodeUsage : byte
    {
        /*----------Individual Stages----------*/

        /// <summary>
        /// Node is able to be included in the pre-process step of the pipeline workflow
        /// </summary>
        PreProcess      = 1 << 0,

        /// <summary>
        /// Node is able to be included in the main process step of the pipeline workflow
        /// </summary>
        Process         = 1 << 1,

        /// <summary>
        /// Node is able to be included in the post-process step of the pipeline workflow
        /// </summary>
        PostProcess     = 1 << 2,

        /*----------Collections----------*/

        /// <summary>
        /// Node is able to be used in all stages of the pipeline workflow
        /// </summary>
        All = PreProcess | Process | PostProcess,

        /// <summary>
        /// Node is able to be used in the support stages of the pipeline workflow
        /// </summary>
        Support = PreProcess | PostProcess
    }
}
