using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Data;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Cancellation;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Control.Linking
{
    /// <summary>
    /// Node that can be used to raise the functionality of a graph on a specific pipeline target to process input
    /// </summary>
    [Node(IsShared = false)]
    public sealed class RunGraphNode : LinkingBaseNode
    {
        /*----------Functions----------*/
        //PROTECTED

        /// <summary>
        /// Retrieve the collection of properties that are needed by the child class to process
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        protected override IList<Property> GetChildInputProperties() => [];

        /// <summary>
        /// Handle the process of raising the required logic for the node, with the base elements worked out for processing
        /// </summary>
        /// <param name="pipelineId">The id of the linked pipeline as requested from the graph</param>
        /// <param name="graphId">The id of the graph on the linked pipeline that should be run</param>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        protected override async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineId pipelineId,
                                                                                   string graphId,
                                                                                   PipelineContext context)
        {
            // Get the base values
            Logger.Log($"[{nameof(RunGraphNode)}] Linking to GraphId={graphId} Pipeline={pipelineId}");
            using var linkCancellationToken = CancellationStack.PushSource(context.CancellationToken);

            // Try to run the linked graph for the required values
            var result = await context.Runner.ExecuteSubProcessAsync
            (
                pipelineId,
                new PipelineContext
                {
                    Runner = context.Runner,
                    CurrentPipeline = context.CurrentPipeline,
                    EnvironmentVariables = context.EnvironmentVariables,
                    RuntimeVariables = context.RuntimeVariables,
                    InputVariables = new Dictionary<string, object?>(),
                    CancellationToken = linkCancellationToken
                },
                graphId
            );
            if (linkCancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult(new TaskCanceledException($"[{nameof(RunGraphNode)}] GraphId={graphId} Pipeline={pipelineId}"));
            }

            // If the process failed, then we have a problem
            if (result.IsError == true)
            {
                Logger.Error($"[{nameof(RunGraphNode)}] Encountered an error while processing GraphId={graphId} Pipeline={pipelineId}\n{result}");
                return result;
            }

            // Handle the output of the operation
            RecordResultValues(result);
            return new ExecutionResult();
        }
    }
}
