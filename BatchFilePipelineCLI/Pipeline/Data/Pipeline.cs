namespace BatchFilePipelineCLI.Pipeline.Data
{
    /// <summary>
    /// Contain the elements that define a pipeline of assets that can be processed to perform the required work
    /// </summary>
    public sealed class Pipeline
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The unique identifier for this pipeline that can be used to reference it for execution and linking
        /// </summary>
        public PipelineId Id { get; private set; }

        /// <summary>
        /// The name that has been assigned to the pipeline for logging
        /// </summary>
        public string? Name { get; private set; }

        /// <summary>
        /// Allow access to the collection of environment variables that exist for the current pipeline
        /// </summary>
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; private set; }

        /// <summary>
        /// The workflow elements that are contained for processing the required work for this pipeline
        /// </summary>
        public Workflow Workflow { get; private set; }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the pipeline with the contained information that can be processed
        /// </summary>
        /// <param name="id">The that can be used to reference this pipeline while processing</param>
        /// <param name="environmentVariables">The collection of environment variables that can be used for processing</param>
        /// <param name="workflow">The workflow elements that can be used to perform the different actions in the </param>
        public Pipeline(PipelineId id,
                        string? name,
                        IReadOnlyDictionary<string, string> environmentVariables,
                        Workflow workflow)
        {
            Id = id;
            Name = name;
            EnvironmentVariables = environmentVariables;
            Workflow = workflow;
        }

        /// <summary>
        /// Get a string representation of the loaded pipeline for operation
        /// </summary>
        public override string ToString() => Name ?? Id.Path;
    }
}
