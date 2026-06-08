using BatchFilePipelineCLI.Pipeline.Data;

namespace BatchFilePipelineCLI.Pipeline.Runners
{
    /// <summary>
    /// An interface that can be used to define an object that can be used to run a pipline
    /// </summary>
    public interface IPipelineRunner
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Handle the running of a specified sub-process within the main execution loop
        /// </summary>
        /// <param name="id">The id of the pipeline asset that is to be run</param>
        /// <param name="context">The context for the currently executing operation</param>
        /// <param name="entryGraphId">[Optional] The id if the graph within the target pipeline that is to be run</param>
        /// <returns>Returns the result object from the running of the process</returns>
        public ValueTask<ExecutionResult> ExecuteSubProcessAsync(PipelineId id,
                                                                 PipelineContext context,
                                                                 string? entryGraphId = null);
    }
}
