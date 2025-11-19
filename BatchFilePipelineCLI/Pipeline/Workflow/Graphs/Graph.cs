using BatchFilePipelineCLI.DynamicProperties;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Utility;
using System.Diagnostics;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Defines a collection of loosly connected nodes that can be run as a graph
    /// </summary>
    internal sealed class Graph
    {
        /*----------Variables----------*/
        //CONST

        /// <summary>
        /// The expected key that will be used to identify the next node that is to be processed
        /// </summary>
        public const string DefaultNextNodeKey = "Default";

        //PRIVATE

        /// <summary>
        /// The initial node of the graph where execution will start from
        /// </summary>
        private readonly NodeDescription? _head;

        /// <summary>
        /// Store the graph nodes that are needed for processing the functional operation
        /// </summary>
        private readonly IDictionary<string/*NodeID*/, IPipelineNode> _graph;

        /// <summary>
        /// Store the descriptions of the different Nodes so we know how to link up information for use
        /// </summary>
        private readonly IDictionary<string/*NodeID*/, NodeDescription> _nodeDescriptions;

        /// <summary>
        /// Store a buffer that can be used to track the different nodes that were used while processing
        /// </summary>
        private readonly List<string> _progressionBuffer = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the graph with the collection of values that will be needed to process functionality
        /// </summary>
        /// <param name="head">The head of the graph where execution will start from</param>
        /// <param name="graph">The collection of keyed nodes that can be evaluated</param>
        /// <param name="descriptions">The collection of keyed descriptions that can be used for parsing</param>
        public Graph(NodeDescription? head,
                     IDictionary<string/*NodeID*/, IPipelineNode> graph,
                     IDictionary<string/*NodeID*/, NodeDescription> descriptions)
        {
            _head = head;
            _graph = graph;
            _nodeDescriptions = descriptions;
        }

        /// <summary>
        /// Handle the execution of the contained graph elements to resolve a result
        /// </summary>
        /// <param name="environmentVariables">The collection of environment variables that can be used when processing node input</param>
        /// <param name="runtimeVariables">The collection of runtime variables that can be used when processing node input</param>
        /// <param name="maxTraversalDepth">The maximum number of steps that can be navigated on the graph before it is cancelled</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the execution of the process</param>
        /// <returns>Returns an output object that contains the result of the graph execution that was needed for processing</returns>
        public async ValueTask<ExecutionResult> ExecuteGraphAsync(IDictionary<string, string?> environmentVariables,
                                                                  IDictionary<string, object?> runtimeVariables,
                                                                  int maxTraversalDepth,
                                                                  CancellationToken cancellationToken)
        {
            // We're going to need some buffers for processing the data
            _progressionBuffer.Clear();
            Dictionary<string, object?> outputResults = new Dictionary<string, object?>();
            Dictionary<string, object?> nodeInputBuffer = new Dictionary<string, object?>();

            // Time the processing operation
            var stopwatch = Stopwatch.StartNew();

            // Step through the graph to process the required data
            NodeDescription? activeNode = _head;
            int steps = 0;
            for (; steps < maxTraversalDepth && activeNode != null; ++steps)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////---------------------------Determine Node Processing----------------------------//////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////

                // Retrieve the node instance that is to be processed
                if (_graph.TryGetValue(activeNode.ID!, out var nodeInstance) == false)
                {
                    return new ExecutionResult
                    (
                        404,
                        $"[{nameof(Graph)}] Failed to retrieve the node instance for '{activeNode}', cannot continue"
                    );
                }
                _progressionBuffer.Add(activeNode.ToString());

                // Determine the type of Node that we are working with
                ExecutionResult nodeOutput = default;
                switch (nodeInstance)
                {
                    // Special case nodes, that have unique, bespoke functionality on the graph
                    case ExportValuesNode exportNode:
                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////----------------------------Resolve Export Variables----------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////

                        // The collection of inputs is defined entirely by the description and will have loose typing
                        foreach (var (name, descriptor) in activeNode.Inputs)
                        {
                            // Try to resolve the descriptor into a value that will be useful
                            if (ArgumentResolver.TryResolveLooseDescriptor(descriptor, environmentVariables, runtimeVariables, out var resolvedInput) == false)
                            {
                                return new ExecutionResult
                                (
                                    422,
                                    $"[{nameof(Graph)}] Couldn't resolve the loose descriptor '{descriptor}' for export"
                                );
                            }

                            // Stash the value for export use
                            outputResults[name] = resolvedInput;
                        }
                        break;

                    // Default nodes, anything else can run as normal
                    default:
                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////-------------------------------Create Node Inputs-------------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////

                        // Find the collection of inputs that are needed for the node
                        nodeInputBuffer.Clear();
                        var nodeInputs = nodeInstance.GetInputProperties();
                        for (int i = 0; i < nodeInputs.Count; ++i)
                        {
                            // Look for a specified descriptor for the input
                            activeNode.Inputs.TryGetValue(nodeInputs[i].Name, out var inputDescriptor);

                            // Try to resolve the description into a value that can be assigned
                            if (ArgumentResolver.TryResolveDescriptor(inputDescriptor, nodeInputs[i], environmentVariables, runtimeVariables, out var resolvedInput) == false)
                            {
                                return new ExecutionResult
                                (
                                    422,
                                    $"[{nameof(Graph)}] Couldn't resolve the descriptor '{inputDescriptor}' for the property '{nodeInputs[i]}'"
                                );
                            }

                            // The node can use this value for processing
                            nodeInputBuffer[nodeInputs[i].Name] = resolvedInput;
                        }

                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////----------------------------------Process Node----------------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////

                        // We can process the node operation and receive the outputs that need to be handled
                        nodeOutput = await nodeInstance.ProcessNodeResultAsync(nodeInputBuffer, cancellationToken);
                        if (cancellationToken.IsCancellationRequested == true)
                        {
                            return new ExecutionResult(new TaskCanceledException($"[{nameof(Graph)}] {activeNode}"));
                        }

                        // If the process failed, we need to stop here
                        if (nodeOutput.IsError == true)
                        {
                            return nodeOutput;
                        }

                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////------------------------------Handle Node Outputs-------------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////

                        // If there are results, then we can process them some way or another
                        if (nodeOutput.Results != null)
                        {
                            // We have the collection of outputs that need to be mapped into the runtime variables for use
                            var nodeOutputs = nodeInstance.GetOutputProperties();
                            for (int i = 0; i < nodeOutputs.Count; ++i)
                            {
                                // See if there is a mapping value for the output
                                if (activeNode.Outputs.TryGetValue(nodeOutputs[i].Name, out var outputMapping) == false ||
                                    string.IsNullOrWhiteSpace(outputMapping) == true)
                                {
                                    continue;
                                }

                                // Check if there is an output value in the result
                                if (nodeOutput.Results.TryGetValue(nodeOutputs[i].Name, out var outputValue) == false)
                                {
                                    continue;
                                }

                                // Assign the output to the runtime variable defined
                                runtimeVariables[outputMapping] = outputValue;
                            }
                        }
                        break;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////-------------------------------Identify Next Node-------------------------------//////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////

                // If there are no connections specified, then we're at the end of the branch
                if (activeNode.Connections.Count == 0)
                {
                    activeNode = null;
                    break;
                }

                // We need to try and find the node that is to be used
                string nextNode = nodeOutput.Next ?? DefaultNextNodeKey;
                if (activeNode.Connections.TryGetValue(nextNode, out var nextNodeId) == false)
                {
                    return new ExecutionResult
                    (
                        404,
                        $"[{nameof(Graph)}] Encountered an error while processing node '{activeNode}'. Unable to find a matching connection for the selection case '{nextNode}'"
                    );
                }

                // If the next node ID is blank, we've also reached the end of the graph (in case switching logic needs to end on a path)
                if (string.IsNullOrWhiteSpace(nextNodeId) == true)
                {
                    activeNode = null;
                    break;
                }

                // Try to find the node description that is to be processed next
                if (_nodeDescriptions.TryGetValue(nextNodeId, out var nextActiveNode) == false)
                {
                    return new ExecutionResult
                    (
                        404,
                        $"[{nameof(Graph)}] Encountered an error while processing node '{activeNode}'. Selected output path was '{nextNode}' which was assigned the ID '{nextNodeId}' but no node in the graph with that ID could be found"
                    );
                }

                // We have the next node in the graph to be processed
                activeNode = nextActiveNode;
            }

            // If we hit the maximum number of steps, nothing we can do
            if (steps == maxTraversalDepth)
            {
                return new ExecutionResult(504, $"[{nameof(Graph)}] Reached the maximum number of steps ({maxTraversalDepth}) while processing the request, failed to complete");
            }

            // We're good
            stopwatch.Stop();
            return new ExecutionResult
            (
                outputResults, 
                additionalDetails: $"[{nameof(Graph)}] Finished executing after: {stopwatch.Elapsed}{(_progressionBuffer.Count > 0 ? $"\n\t{string.Join("\n\t", _progressionBuffer.Select((v, i) => $"{i}.\t{v}"))}" : string.Empty)}"
            );
        }
    }
}
