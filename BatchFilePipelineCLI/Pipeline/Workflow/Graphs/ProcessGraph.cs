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
        /// The default maximum number of node steps that can be made before the process is killed
        /// </summary>
        private const int DEFAULT_MAX_TRAVERSAL_STEPS = 25;

        //PROTECTED

        /// <summary>
        /// Define a property that can be used to identify the maximum traversal depth for the graph of elements
        /// </summary>
        protected readonly ArgumentDescription _maxTraversalDepthProperty = new ArgumentDescription
        (
            "MaxTraversalDepth",
            "The maximum number of node steps that can be made when processing a graph before the process is killed",
            typeof(int),
            required: false,
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

            // Progress through the graph while there are connections to follow
            NodeDescription? activeNode = _primaryNode;
            int steps = 0;
            for (; steps < DEFAULT_MAX_TRAVERSAL_STEPS && activeNode is not null; ++steps)
            {

            }

            // If we reached the upper limit, that's a problem
            if (steps == DEFAULT_MAX_TRAVERSAL_STEPS)
            {
                Logger.Error($"{_graphName} reached the maximum number of steps ({DEFAULT_MAX_TRAVERSAL_STEPS}) while processing the request, failed to complete");
                return -1;
            }

            // If we made it this far, we're good
            stopwatch.Stop();
            Logger.Log($"{_graphName} completed execution after: {stopwatch.Elapsed}\n\t{string.Join("\n\t", _progressionBuffer.Select((v, i) => $"{i}.\t{v}"))}");
            return 0;
        }
    }
}
