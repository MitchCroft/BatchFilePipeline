using BatchFilePipelineCLI.Pipeline.Data;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Data;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Control.Linking
{
    /// <summary>
    /// Define the common functionality that is needed for a node that links to another graph/pipeline
    /// </summary>
    public abstract class LinkingBaseNode : INode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need a number of different inputs to know how to process the data that is being passed in
        /// </summary>
        private readonly Property _pipelineProperty = Property.Create
        (
            "Pipeline",
            "The path to the pipeline asset that is to be run when processing the request. If empty, will look in the current pipeline",
            PipelineId.Empty,
            "Lib/Math.xml"
        );
        private readonly Property _graphIdProperty = Property.Create
        (
            "GraphId",
            "The ID of the graph within the defined pipeline that will be run with the required values. If empty, will use the default entry point for the pipeline",
            string.Empty,
            "AddGraph"
        );

        /// <summary>
        /// Create a dynamnic collection of buffers that can be used for processing node results
        /// </summary>
        private readonly DynamicDataBuffer<object> _bufferPool = new DynamicDataBuffer<object>(0, 1, x => x.Clear());

        /// <summary>
        /// Track the intermediate results that are output from the running graph elements that are needed for a consolidated return result
        /// </summary>
        private readonly Dictionary<string, DynamicDataBufferInstance<object>> _processingResults = new();

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
        public IList<Property> GetInputProperties()
        {
            var childProperties = GetChildInputProperties();
            Property[] combined = new Property[childProperties.Count + 2];
            combined[0] = _pipelineProperty;
            combined[1] = _graphIdProperty;
            childProperties.CopyTo(combined, 2);
            return combined;
        }

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
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineContext context)
        {
            try
            {
                // Setup the base values that are needed for processing
                PipelineId pipeline = context.GetInput<PipelineId>(_pipelineProperty);
                string graphId = context.GetInput<string>(_graphIdProperty);

                // Get the result of the child process
                var result = await ProcessNodeResultAsync(pipeline, graphId, context);
                if (context.CancellationToken.IsCancellationRequested == true)
                {
                    return new ExecutionResult(new TaskCanceledException($"[{nameof(LinkingBaseNode)}] GraphId={graphId} Pipeline={pipeline}"));
                }
                if (result.IsError == true)
                {
                    return result;
                }

                // We want to isolate the values that are needed for the final output
                _outputValues.Clear();
                foreach (var (resultKey, resultList) in _processingResults)
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

            // Clear the data buffers that are no longer needed
            finally
            {
                foreach (var buffer in _processingResults.Values)
                {
                    buffer.Dispose();
                }
                _processingResults.Clear();
            }
        }

        //PROTECTED

        /// <summary>
        /// Retrieve the collection of properties that are needed by the child class to process
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        protected abstract IList<Property> GetChildInputProperties();

        /// <summary>
        /// Handle the process of raising the required logic for the node, with the base elements worked out for processing
        /// </summary>
        /// <param name="pipelineId">The id of the linked pipeline as requested from the graph</param>
        /// <param name="graphId">The id of the graph on the linked pipeline that should be run</param>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        protected abstract ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineId pipelineId,
                                                                             string graphId,
                                                                             PipelineContext context);

        /// <summary>
        /// Process the incoming result object and record the results that are stored within it
        /// </summary>
        /// <param name="result">The result object that is to be processed</param>
        protected void RecordResultValues(ExecutionResult result)
        {
            // Check there are results to record
            if (result.Results is null)
            {
                return;
            }

            // Store the result values that will be able to be passed back at the end of the processing
            foreach (var (resultKey, resultValue) in result.Results)
            {
                // See if we have an existing list of result values
                if (_processingResults.TryGetValue(resultKey, out var resultList) == false)
                {
                    _processingResults[resultKey] = resultList = _bufferPool.Rent();
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
    }
}
