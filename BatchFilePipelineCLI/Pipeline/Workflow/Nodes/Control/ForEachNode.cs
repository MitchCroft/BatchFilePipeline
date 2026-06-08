using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control
{
    /// <summary>
    /// Special purpose node that can be used to iterate over a collection of items and run a specified sub-graph for each item in the collection,
    /// with the current item being made available as a runtime variable for the sub-graph to use
    /// </summary>
    [PipelineNode(nameof(ForEachNode), NodeUsage.Process, IsShared = false)]
    internal sealed class ForEachNode : INode
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
        private readonly Property _subProcessIdProperty = Property.Create
        (
            "SubProcessId",
            "The ID of the sub-process graph that will be run with the corresponding elements that are to be processed",
            typeof(string)
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

        /// <summary>
        /// Store a collection of the output values that are to be made available to the higher level graph after processing is complete
        /// </summary>
        private readonly Dictionary<string, object> _outputValues = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_collectionProperty, _subProcessIdProperty, _indexVariableNameProperty, _valueVariableNameProperty];

        /// <summary>
        /// We need to dynamically create the properties that will be used for the output based on the results that are generated from processing the sub-graph
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties()
        {
            List<Property> outputProperties = new(_outputValues.Count);
            foreach (var (key, value) in _outputValues)
            {
                outputProperties.Add(Property.Create
                (
                    key,
                    "A runtime created property that can be used in later stages of processing",
                    value.GetType()
                ));
            }
            return outputProperties;
        }

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                       CancellationToken cancellationToken)
        {
            // Retrieve the values that will be used for processing
            _outputValues.Clear();
            IEnumerable collection = context.GetInput<IEnumerable>(_collectionProperty);
            string subProcessId = context.GetInput<string>(_subProcessIdProperty);
            string indexVariableName = context.GetInput<string>(_indexVariableNameProperty);
            string valueVariableName = context.GetInput<string>(_valueVariableNameProperty);

            // Try to find the sub-process graph that can be used for processing the elements in the collection
            Logger.Log($"[{nameof(ForEachNode)}] Looking for the Sub-Process Graph with the ID '{subProcessId}'");
            GraphRunner? subProcessGraph = context.GetSubProcess?.Invoke(subProcessId);
            if (subProcessGraph is null)
            {
                return new ExecutionResult
                (
                    404,
                    $"Unable to find a Sub-Process Graph with the ID '{subProcessId}'"
                );
            }

            // Iterate over and process all of the elements in the collection
            int index = 0;
            Dictionary<string, List<object>> processingResults = new();
            foreach (var value in collection)
            {
                // Setup the runtime variables that will be needed for processing the current element
                Dictionary<string, object?> localScopeRuntime = new Dictionary<string, object?>(context.RuntimeVariables);
                localScopeRuntime[indexVariableName] = index++;
                localScopeRuntime[valueVariableName] = value;

                // Run the graph for the current element
                var result = await subProcessGraph.ExecuteGraphAsync
                (
                    localScopeRuntime,
                    context.GetSubProcess,
                    cancellationToken
                );
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return new ExecutionResult();
                }

                // If the process failed, then we have a problem
                if (result.IsError == true)
                {
                    Logger.Error($"[{nameof(ForEachNode)}] Encountered an error while processing '{value}' with the sub-process graph '{subProcessId}'\n{result}");
                    return result;
                }

                // Check if there are any export values that need to be passed back up
                if (result.Results == null)
                {
                    continue;
                }

                // Store the result values that will be able to be passed back at the end of the processing
                foreach (var (resultKey, resultValue) in result.Results)
                {
                    // See if we have an existing list of result values
                    if (processingResults.TryGetValue(resultKey, out var resultList) == false)
                    {
                        processingResults[resultKey] = resultList = new(1);
                    }

                    // Check how the values should be added to the list of results
                    if (resultValue is not string &&
                        resultValue is IEnumerable resultEnumerable)
                    {
                        foreach (var resultEnumerableValue in resultEnumerable)
                        {
                            if (resultEnumerableValue == null)
                            {
                                continue;
                            }
                            resultList.Add(resultEnumerableValue);
                        }
                    }
                    else if (resultValue != null)
                    {
                        resultList.Add(resultValue);
                    }
                }
            }

            // We want to isolate the values that are needed for the final output
            foreach (var (resultKey, resultList) in processingResults)
            {
                // If there is a single value, then we can just store that value directly
                if (resultList.Count == 1)
                {
                    _outputValues[resultKey] = resultList[0];
                    continue;
                }

                // If there are multiple values, then we will need to know how to store them
                var groupEnumerable = resultList.GroupBy(x => x.GetType());

                // If there are multiple types included, then just use the list object we already have
                if (groupEnumerable.Count() > 1)
                {
                    _outputValues[resultKey] = resultList;
                    continue;
                }

                // Otherwise, we can create a typed array for the values that contained
                var typedArray = Array.CreateInstance(resultList[0].GetType(), resultList.Count);
                for (int i = 0; i < resultList.Count; ++i)
                {
                    typedArray.SetValue(resultList[i], i);
                }
                _outputValues[resultKey] = typedArray;
            }

            // We have our final results from the Node operation that can be processed
            return new ExecutionResult(_outputValues!);
        }
    }
}
