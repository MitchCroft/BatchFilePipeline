using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using BatchFilePipelineCLI.Utility.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Utility class that can be used to create a graph that can be run to process the required information
    /// </summary>
    internal static class GraphRunnerBuilder
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create a graph runner from the provided description for use
        /// </summary>
        /// <param name="description">The description of the graph that is to be processed</param>
        /// <param name="library">The library of nodes that are available for use</param>
        /// <param name="validNodes">A mask of the types of nodes that should be allowed to exist on the graph</param>
        /// <param name="environmentVariables">The collection of environment variables that are available for use</param>
        /// <param name="argumentVariables">The collection of command line argument variables that have been supplied to the program for use</param>
        /// <param name="graphRunner">Passes out an instance of the graph runner that can be used to perform work, if it could be created</param>
        /// <returns>Returns true if the graph runner could be built for use</returns>
        public static bool TryBuildGraphRunner(GraphDescription description,
                                               NodeLibrary library,
                                               NodeUsage validNodes,
                                               IDictionary<string, string?> environmentVariables,
                                               IDictionary<string, string?> argumentVariables,
                                               [NotNullWhen(true)] out GraphRunner? graphRunner)
        {
            // Try to build the graph that will be processed
            if (TryBuildGraph(description.Nodes ?? Array.Empty<NodeDescription>(), library, validNodes, out var graph) == false)
            {
                graphRunner = null;
                return false;
            }

            // Create the graph runner with the available values
            graphRunner = new GraphRunner
            (
                graph,
                environmentVariables.Merge(description.Environment ?? new KeyValueSection(), argumentVariables)
            );
            return true;
        }

        //PRIVATE

        /// <summary>
        /// Try to construct a graph that can be processed to perform work
        /// </summary>
        /// <param name="nodes">The collection of nodes that are to be used for processing</param>
        /// <param name="library">The library lookup that will be used to identify the nodes needed</param>
        /// <param name="validNodes">A mask of the types of nodes that should be allowed to exist on the graph</param>
        /// <param name="graph">Passes out the graph if able to be built or null if unable to create one</param>
        /// <returns>Returns true if the graph was able to be built</returns>
        private static bool TryBuildGraph(IList<NodeDescription> nodes,
                                         NodeLibrary library,
                                         NodeUsage validNodes,
                                         [NotNullWhen(true)] out Graph? graph)
        {
            // We need to parse the descriptions to find the elements that will be used to create the graph
            NodeDescription? head = null;
            Dictionary<string/*NodeID*/, IPipelineNode> graphNodes = new();
            Dictionary<string/*NodeID*/, NodeDescription> nodeDescriptions = new();

            // Loop through all of the supplied ndoes and see if we can use them
            bool success = true;
            for (int i = 0; i < nodes.Count; ++i)
            {
                // This node needs to have a valid ID that can be used
                if (string.IsNullOrWhiteSpace(nodes[i].ID) == true)
                {
                    Logger.Error($"[{nameof(GraphRunnerBuilder)}] Failed when parsing the node at index {i} '{nodes[i]}', there is no ID assigned");
                    success = false;
                    continue;
                }

                // Check that the ID is unique and can be used
                if (nodeDescriptions.TryGetValue(nodes[i].ID!, out var previousNode) == true)
                {
                    Logger.Error($"[{nameof(GraphRunnerBuilder)}] Failed when parsing the node '{nodes[i]}' at index {i}, the ID '{nodes[i].ID}' is already in use by '{previousNode.Name}'");
                    success = false;
                    continue;
                }

                // Check that there is a type ID available
                if (string.IsNullOrWhiteSpace(nodes[i].TypeID) == true)
                {
                    Logger.Error($"[{nameof(GraphRunnerBuilder)}] Failed when parsing the node '{nodes[i]}' at index {i}, no {nameof(NodeDescription.TypeID)} value could be found");
                    success = false;
                    continue;
                }

                // Check that we can use a node of this type
                if (library.TryGetNodeCharacteristics(nodes[i].TypeID!, out var nodeCharacteristics) == false)
                {
                    Logger.Error($"[{nameof(GraphRunnerBuilder)}] Failed when parsing the node '{nodes[i]}' at index {i}, the characteristics for Type ID '{nodes[i].TypeID}' couldn't be found");
                    success = false;
                    continue;
                }

                // Check that this node can be used on the graph
                if ((nodeCharacteristics.UsageFlags & validNodes) == 0)
                {
                    Logger.Error($"[{nameof(GraphRunnerBuilder)}] Failed when parsing the node '{nodes[i]}' at index {i}, the node Type ID '{nodes[i].TypeID}' is not valid for use in this graph");
                    success = false;
                    continue;
                }

                // Try to get the instance of the node that will be used
                if (library.TryGetInstanceOfNode(nodes[i].TypeID!, out var nodeInstance) == false)
                {
                    Logger.Error($"[{nameof(GraphRunnerBuilder)}] Failed when parsing the node '{nodes[i]}' at index {i}, an instance of Type ID '{nodes[i].TypeID}' couldn't be created");
                    success = false;
                    continue;
                }

                // Store the values for processing later
                graphNodes[nodes[i].ID!] = nodeInstance;
                nodeDescriptions[nodes[i].ID!] = nodes[i];

                // If this is the first node, it'll be the head of the graph
                if (i == 0)
                {
                    head = nodes[i];
                }
            }

            // If we encountered a fail, nothing we can do
            if (success == false)
            {
                graph = null;
                return false;
            }

            // We can build the graph that will be used
            graph = new Graph(head, graphNodes, nodeDescriptions);
            return true;
        }
    }
}
