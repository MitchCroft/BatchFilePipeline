namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// Defines the common elements that are shared by elements in a pipeline
    /// </summary>
    public interface IWorkflowElement
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Human readable name of the element that can be used to describe it in a pipeline
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The unique ID of the element within the pipeline that is being processed
        /// </summary>
        public string? Id { get; }
    }
}
