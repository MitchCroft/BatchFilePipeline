using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Data;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Cancellation;
using System.Collections;
using System.Diagnostics;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Control.Linking
{
    /// <summary>
    /// Special purpose node that can be used to iterate over a collection of items and run a specified sub-graph for each item in the collection,
    /// with the current item being made available as a runtime variable for the sub-graph to use
    /// </summary>
    [Node(IsShared = false)]
    public sealed class ForEachNode : LinkingBaseNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need a number of different inputs to know how to process the data that is being passed in
        /// </summary>
        private readonly Property _collectionProperty = Property.Create
        (
            "Collection",
            "The collection of values that are to be processed as a part of the comparison",
            typeof(IEnumerable)
        );
        private readonly Property _indexVariableNameProperty = Property.Create
        (
            "IndexVariableName",
            "The name that will be given to the runtime variable that contains the current index of the element being processed in the collection",
            "LoopIndex"
        );
        private readonly Property _valueVariableNameProperty = Property.Create
        (
            "ValueVariableName",
            "The name that will be given to the runtime variable that contains the current value of the element being processed in the collection",
            "LoopValue"
        );
        private readonly Property _cancelOnErrorProperty = Property.Create
        (
            "CancelOnError",
            "Flags if the loop should be abandoned if an error occurrs while processing",
            true
        );
        private readonly Property _processIndividualCancelProperty = Property.Create
        (
            "ProcessIndividualCancel",
            "Flags if the cancel operation input should only cancel the actively running element being processed instead of the whole loop",
            false
        );
        private readonly Property _generateReportProperty = Property.Create
        (
            "GenerateReport",
            "Flags if a report of the elements that were processed should be generated while operating for information generating",
            false
        );

        /*----------Functions----------*/
        //PROTECTED

        /// <summary>
        /// Retrieve the collection of properties that are needed by the child class to process
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        protected override IList<Property> GetChildInputProperties() => [_collectionProperty, _indexVariableNameProperty, _valueVariableNameProperty, _cancelOnErrorProperty, _processIndividualCancelProperty, _generateReportProperty];

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
            // Retrieve the values that will be used for processing
            IEnumerable collection = context.GetInput<IEnumerable>(_collectionProperty);
            string indexVariableName = context.GetInput<string>(_indexVariableNameProperty);
            string valueVariableName = context.GetInput<string>(_valueVariableNameProperty);
            bool cancelOnError = context.GetInput<bool>(_cancelOnErrorProperty);
            bool processIndividualCancel = context.GetInput<bool>(_processIndividualCancelProperty);
            bool generateReport = context.GetInput<bool>(_generateReportProperty);
            Logger.Log($"[{nameof(ForEachNode)}] Linking to GraphId={graphId} Pipeline={pipelineId}");

            // If we're creating a report, we will need some elements
            Stopwatch? timer = null;
            List<object?>? completedSuccessfully = null;
            List<object?>? completedFailure = null;
            int failureCount = 0;
            if (generateReport == true)
            {
                completedSuccessfully = new List<object?>();
                completedFailure = new List<object?>();
                timer = Stopwatch.StartNew();
            }

            // Iterate over and process all of the elements in the collection
            int index = 0;
            foreach (var value in collection)
            {
                // Setup the runtime variables that will be needed for processing the current element
                Dictionary<string, object?> localScopeRuntime = new Dictionary<string, object?>(context.RuntimeVariables);
                if (string.IsNullOrWhiteSpace(indexVariableName) == false)
                {
                    localScopeRuntime[indexVariableName] = index++;
                }
                if (string.IsNullOrWhiteSpace(valueVariableName) == false)
                {
                    localScopeRuntime[valueVariableName] = value;
                }

                // Get a unique cancellation token for this element
                CancellationToken elementCancellationToken = context.CancellationToken;
                if (processIndividualCancel == true)
                {
                    elementCancellationToken = CancellationStack.PushSource(elementCancellationToken);
                }

                // Try to run the linked graph for the required values
                var result = await context.Runner.ExecuteSubProcessAsync
                (
                    pipelineId,
                    new PipelineContext
                    {
                        Runner = context.Runner,
                        CurrentPipeline = context.CurrentPipeline,
                        EnvironmentVariables = context.EnvironmentVariables,
                        RuntimeVariables = localScopeRuntime,
                        InputVariables = new Dictionary<string, object?>(),
                        CancellationToken = elementCancellationToken
                    },
                    graphId
                );

                // Try to clear the individual processing element
                if (processIndividualCancel == true)
                {
                    // Remove the token from the stack
                    CancellationStack.PopSource(elementCancellationToken);

                    // Check to see if the operation has been cancelled
                    if (elementCancellationToken.IsCancellationRequested == true &&
                        context.CancellationToken.IsCancellationRequested == false)
                    {
                        continue;
                    }
                }

                // If the root element has been cancelled, kill it
                if (context.CancellationToken.IsCancellationRequested == true)
                {
                    return new ExecutionResult(new TaskCanceledException($"[{nameof(ForEachNode)}] Index={index} Value={value}"));
                }

                // If the process failed, then we have a problem
                if (result.IsError == true)
                {
                    ++failureCount;
                    Logger.Error($"[{nameof(ForEachNode)}] Encountered an error while processing '{value}' with the GraphId={graphId} Pipeline={pipelineId}\n{result}");
                    if (cancelOnError == true)
                    {
                        return result;
                    }
                    completedFailure?.Add(value);
                    continue;
                }

                // Record the results we got for this iteration
                completedSuccessfully?.Add(value);
                RecordResultValues(result);
            }

            // Check if there's a report we need to output
            if (generateReport == true)
            {
                timer!.Stop();
                int total = completedFailure!.Count + completedSuccessfully!.Count;
                Logger.Log($"==================== RUN SUMMARY ====================\n\tGraphId={graphId}\n\tPipeline={pipelineId}\n\tProcessed Count={total}\n\tDuration={timer.Elapsed}\n");
                if (total == 0)
                {
                    Logger.Warning("Unable to find any elements to process!");
                }
                if (completedSuccessfully.Count > 0)
                {
                    float successRate = completedSuccessfully.Count / (float)total;
                    Logger.Success($"Success {completedSuccessfully.Count}/{total} ({successRate:P})\n\t{string.Join("\n\t", completedSuccessfully)}");
                }
                if (completedFailure.Count > 0)
                {
                    float failureRate = completedFailure.Count / (float)total;
                    Logger.Error($"Failed {completedFailure.Count}/{total} ({failureRate:P})\n\t{string.Join("\n\t", completedFailure)}");
                }
                Logger.Log($"==================== END SUMMARY ====================");
            }

            // We have a successful flag that we can emit
            SetResult("LoopSuccessful", failureCount == 0);

            // We've got the final result
            return new ExecutionResult
            (
                new Dictionary<string, object?>(),
                nextNode: completedFailure?.Count > 0 ? "Failure" : null
            );
        }
    }
}
