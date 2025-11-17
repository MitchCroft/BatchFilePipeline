using BatchFilePipelineCLI.DynamicProperties;
using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Nodes;
using System.Diagnostics;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Base class for a graph that handles the execution of Node functions in a sequence to product an output
    /// </summary>
    internal abstract class ProcessGraph
    {
        /*----------Variables----------*/
        //CONST

        /// <summary>
        /// The expected key that will be used to identify the next node that is to be processed
        /// </summary>
        private const string DefaultNextNodeKey = "Default";

        //PROTECTED

        /// <summary>
        /// Define a property that can be used to identify the maximum traversal depth for the graph of elements
        /// </summary>
        protected readonly Property _maxTraversalDepthProperty = Property.Create
        (
            "maxTraversalDepth",
            "The maximum number of node steps that can be made when processing a graph before the process is killed",
            defaultValue: 25
        );

        //PRIVATE

        /// <summary>
        /// The name of the graph that will be used for debug logging
        /// </summary>
        private readonly string _graphName;

        /// <summary>
        /// Store the graph nodes that are needed for processing the functional operation
        /// </summary>
        private readonly Dictionary<string/*NodeID*/, IPipelineNode> _graph = new();

        /// <summary>
        /// Store the descriptions of the different Nodes so we know how to link up information for use
        /// </summary>
        private readonly Dictionary<string/*NodeID*/, NodeDescription> _nodeDescriptions = new();

        /// <summary>
        /// Store a buffer that can be used to track the different nodes that were used while processing
        /// </summary>
        private readonly List<string> _progressionBuffer = new();

        /// <summary>
        /// This is the root, starting node that will be run via the graph
        /// </summary>
        private NodeDescription? _primaryNode = null;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Attempt to initialise the graph object for processing data
        /// </summary>
        /// <param name="nodes">The collection of Node description elements that make up the graph that is to be processed</param>
        /// <param name="library">The node library that can be used to retrieve instances of the backing nodes required for operation</param>
        /// <param name="environmentVariables">The collection of environment variables that describe the operational state of the program</param>
        /// <returns>Returns true if at the surface level the graph is valid and can be operated</returns>
        public bool TryInitialiseGraph(IList<NodeDescription> nodes,
                                       NodeLibrary library,
                                       Dictionary<string, string?> environmentVariables)
        {
            return InitialiseGraphNodes(nodes, library) &&
                IdentifyStartingValues(environmentVariables);
        }

        //PROTECTED

        /// <summary>
        /// Create the graph with the required values
        /// </summary>
        /// <param name="graphName">The name that will be applied to the graph for display</param>
        protected ProcessGraph(string graphName)
        {
            _graphName = graphName;
        }

        /// <summary>
        /// Try to load the values that are needed for processing a graph of elements
        /// </summary>
        /// <param name="nodes">The collection of Node description elements that make up the graph that is to be processed</param>
        /// <returns>Returns true if the node elements could be loaded successfully for use</returns>
        protected bool InitialiseGraphNodes(IList<NodeDescription> nodes, NodeLibrary library)
        {
            // Clear previous values
            _graph.Clear();
            _nodeDescriptions.Clear();
            _primaryNode = null;

            // Look through all of the supplied nodes and see if we can use them
            bool success = true;
            for (int i = 0; i < nodes.Count; ++i)
            {
                // This node needs to have a valid ID that can be used
                if (string.IsNullOrWhiteSpace(nodes[i].ID) == true)
                {
                    Logger.Error($"Failed when parsing the node at index {i} '{nodes[i].Name}', there is no ID assigned");
                    success = false;
                    continue;
                }

                // Check that there is a type ID available
                if (string.IsNullOrWhiteSpace(nodes[i].TypeID) == true)
                {
                    Logger.Error($"Failed when parsing the node '{nodes[i].Name}' ({nodes[i].ID}) at index {i}, no {nameof(NodeDescription.TypeID)} value could be found");
                    success = false;
                    continue;
                }

                // Check that the ID is unique and can be used
                if (_nodeDescriptions.TryGetValue(nodes[i].ID!, out var previousNode) == true)
                {
                    Logger.Error($"Failed when parsing the node '{nodes[i].Name}' ({nodes[i].ID}) at index {i}, the ID '{nodes[i].ID}' is already in use by '{previousNode.Name}'");
                    success = false;
                    continue;
                }

                // Try to get the instance of the node that will be used
                if (library.TryGetInstanceOfNode(nodes[i].TypeID!, out var nodeInstance) == false)
                {
                    Logger.Error($"Failed when parsing the node '{nodes[i].Name}' ({nodes[i].ID}) at index {i}, the Type ID '{nodes[i].TypeID}' couldn't be found");
                    success = false;
                    continue;
                }

                // Store the values for processing
                _graph[nodes[i].ID!] = nodeInstance;
                _nodeDescriptions[nodes[i].ID!] = nodes[i];

                // If this is the first node, it'll be the primary
                if (i == 0)
                {
                    _primaryNode = nodes[i];
                }
            }
            return success;
        }

        /// <summary>
        /// Check to see that all of the starting information is defined for the operation to progress
        /// </summary>
        /// <param name="environmentVariables">The collection of environment variables that have been defined for use</param>
        /// <returns>Returns true if the required information is present and available for processing</returns>
        protected abstract bool IdentifyStartingValues(Dictionary<string, string?> environmentVariables);

        /// <summary>
        /// Handle the execution of the graph
        /// </summary>
        /// <param name="environmentVariables">The collection of environment variables that can be used when processing node input</param>
        /// <param name="runtimeVariables">The collection of runtime variables that can be used when processing node input</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the execution of the process</param>
        /// <returns>Returns the status code of the operation once it has finished running</returns>
        protected async ValueTask<int> ExecuteGraphAsync(Dictionary<string, string?> environmentVariables,
                                                         Dictionary<string, object?> runtimeVariables,
                                                         CancellationToken cancellationToken)
        {
            // Clear the current progression buffer for the new round
            _progressionBuffer.Clear();

            // We can time the duration of the graph execution
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Find the traversal depth limit from the properties
            if (ArgumentResolver.TryResolveEnvironmentVariable(_maxTraversalDepthProperty, environmentVariables, out int maxTraversalDepth) == false)
            {
                Logger.Error($"[{nameof(ProcessGraph)}] {_graphName} failed to resolve the maximum traversal depth from the environment variables, cannot continue");
                return -1;
            }

            // We're going to need a collection of inputs that can be used to process each node
            Dictionary<string, object?> nodeInputBuffer = new();

            // Progress through the graph while there are connections to follow
            NodeDescription? activeNode = _primaryNode;
            int steps = 0;
            for (; steps < maxTraversalDepth && activeNode is not null; ++steps)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////-------------------------------Create Node Inputs-------------------------------//////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////

                // Retrieve the node instance that is to be processed
                if (_graph.TryGetValue(activeNode.ID!, out var nodeInstance) == false)
                {
                    Logger.Error($"[{nameof(ProcessGraph)}] {_graphName} failed to retrieve the node instance for '{activeNode.Name}' ({activeNode.ID}), cannot continue");
                    return 404;
                }

                // Find the collection of inputs needed for the node
                nodeInputBuffer.Clear();
                var nodeInputs = nodeInstance.GetInputProperties();
                for (int i = 0; i < nodeInputs.Count; ++i)
                {
                    // Look for a specified descriptor for the input
                    activeNode.Inputs.TryGetValue(nodeInputs[i].Name, out var inputDescriptor);

                    // Try to resolve the description into a value that can be assigned
                    if (ArgumentResolver.TryResolveDescription(inputDescriptor, nodeInputs[i], environmentVariables, runtimeVariables, out var resolvedInput) == false)
                    {
                        Logger.Error($"[{nameof(ProcessGraph)}] {_graphName} couldn't resolve the descriptor '{inputDescriptor}' for the property '{nodeInputs[i]}'");
                        return 422;
                    }

                    // Store the resolved value for use
                    nodeInputBuffer[nodeInputs[i].Name] = resolvedInput;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////----------------------------------Process Node----------------------------------//////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////

                // We can process the node operation and receive the outputs that need to be handled
                var nodeOutput = await nodeInstance.ProcessNodeResultAsync(nodeInputBuffer, cancellationToken);
                if (cancellationToken.IsCancellationRequested == true)
                {
                    Logger.Warning($"[{nameof(ProcessGraph)}] {_graphName} operation was cancelled while processing node '{activeNode.Name}' ({activeNode.ID})");
                    return 499;
                }

                // If the process failed, we need to stop here
                if (nodeOutput.IsError == true)
                {
                    Logger.Error($"[{nameof(ProcessGraph)}] {_graphName} encountered an error while processing node '{activeNode.Name}' ({activeNode.ID})\n{nodeOutput}");
                    return nodeOutput.ResultCode;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////------------------------------Handle Node Outputs-------------------------------//////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////

                // Check there are outputs to be added to the runtime variables
                if (nodeOutput.Results != null)
                {
                    // We have the collection of outputs that need to be be mapped into the runtime variables for use
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
                            Logger.Warning($"[{nameof(ProcessGraph)}] {_graphName} couldn't find an output value for the property '{nodeOutputs[i]}' from node '{activeNode.Name}' ({activeNode.ID})");
                            continue;
                        }

                        // Assign the output to the runtime variable defined
                        runtimeVariables[outputMapping] = outputValue;
                    }
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
                string nextNode = nodeOutput.NextNode ?? DefaultNextNodeKey;
                if (activeNode.Connections.TryGetValue(nextNode, out var nextNodeId) == false)
                {
                    Logger.Error($"[{nameof(ProcessGraph)}] {_graphName} encountered an error while processing node '{activeNode.Name}' ({activeNode.ID}). Unable to find a matching connection for the selection case '{nextNode}'");
                    break;
                }

                // If the next node ID is blank, we've also reach the end of the graph (in case switching logic needs to end on a path)
                if (string.IsNullOrWhiteSpace(nextNodeId) == true)
                {
                    activeNode = null;
                    break;
                }

                // Try to find the node description that is to be processed next
                if (_nodeDescriptions.TryGetValue(nextNodeId, out var nextActiveNode) == false)
                {
                    Logger.Error($"[{nameof(ProcessGraph)}] {_graphName} encountered an error while processing node '{activeNode}' ({activeNode.ID}). Selected output path was '{nextNode}' which was assigned the ID '{nextNodeId}' but no node in the graph with that ID could be found");
                    break;
                }

                // We have the next node in the graph to be processed
                activeNode = nextActiveNode;
            }

            // If we reached the upper limit, that's a problem
            if (steps == maxTraversalDepth)
            {
                Logger.Error($"{_graphName} reached the maximum number of steps ({maxTraversalDepth}) while processing the request, failed to complete");
                return -1;
            }

            // If we made it this far, we're good
            stopwatch.Stop();
            Logger.Log($"{_graphName} completed execution after: {stopwatch.Elapsed}\n\t{string.Join("\n\t", _progressionBuffer.Select((v, i) => $"{i}.\t{v}"))}");
            return 0;
        }
    }
}
