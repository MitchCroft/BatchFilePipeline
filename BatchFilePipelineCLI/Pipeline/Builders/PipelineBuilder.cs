using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Data;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Utility.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Pipeline.Builders
{
    /// <summary>
    /// Handle the construction of the pipeline object that will be executed while processing the 
    /// </summary>
    public static class PipelineBuilder
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Try to read from disk and construct the pipeline that can be run to perform the work
        /// </summary>
        /// <param name="pipelinePath">The path to the root pipeline asset that will be used to construct the pipeline</param>
        /// <param name="rootPath">The root path that can be used when constructing the final path for the asset</param>
        /// <param name="nodeLibrary">The library of available nodes that can be used for execution and construction</param>
        /// <param name="environmentVariables">The collection of environment variables that have been defined for the running application</param>
        /// <param name="argumentVariables">The collection of argument variables that have been provided to the running application</param>
        /// <param name="pipeline">Passes out the pipeline that has been constructed from the specified path, or null if unable to</param>
        /// <returns>Returns true if the pipeline was able to be constructed successfully</returns>
        public static bool TryBuildPipeline(string pipelinePath,
                                            string rootPath,
                                            NodeLibrary nodeLibrary,
                                            IReadOnlyDictionary<string, string> environmentVariables,
                                            IReadOnlyDictionary<string, string> argumentVariables,
                                            [NotNullWhen(true)] out Data.Pipeline? pipeline) =>
            TryBuildPipeline
            (
                new PipelineId(pipelinePath, rootPath),
                nodeLibrary,
                environmentVariables,
                argumentVariables,
                out pipeline
            );

        /// <summary>
        /// Try to read from disk and construct the pipeline that can be run to perform the work
        /// </summary>
        /// <param name="id">The id of the pipeline that is to be read in and processed</param>
        /// <param name="nodeLibrary">The library of available nodes that can be used for execution and construction</param>
        /// <param name="environmentVariables">The collection of environment variables that have been defined for the running application</param>
        /// <param name="argumentVariables">The collection of argument variables that have been provided to the running application</param>
        /// <param name="pipeline">Passes out the pipeline that has been constructed from the specified path, or null if unable to</param>
        /// <returns>Returns true if the pipeline was able to be constructed successfully</returns>
        public static bool TryBuildPipeline(PipelineId id,
                                            NodeLibrary nodeLibrary,
                                            IReadOnlyDictionary<string, string> environmentVariables,
                                            IReadOnlyDictionary<string, string> argumentVariables,
                                            [NotNullWhen(true)] out Data.Pipeline? pipeline)
        {
            // Try to open the pipeline file for processing
            if (PipelineDescription.TryOpen(id.Path, out var pipelineDescription) == false)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Failed to open the pipeline description from the path '{id}'");
                pipeline = null;
                return false;
            }

            // There are base level environment variables that can be used while processing
            var pipelineEnvironmentVariables = environmentVariables
                .Merge(pipelineDescription.Environment, argumentVariables);

            // Try to build the workflow that will be used for actually processing work
            if (TryBuildWorkflow(pipelineDescription.Workflow, nodeLibrary, out var workflow) == false)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Encounred errors while trying to build the workflow for the pipeline description '{pipelineDescription}'");
                pipeline = null;
                return false;
            }

            // If we got this far, then structurally at least there is nothing wrong with the pipeline
            pipeline = new Data.Pipeline(id, pipelineDescription.Name, pipelineEnvironmentVariables, workflow);
            return true;
        }

        //PRIVATE

        /// <summary>
        /// Try to build out the workflow from the specified description
        /// </summary>
        /// <param name="workflowDescription">The description of the workflow that is being built</param>
        /// <param name="nodeLibrary">The library of nodes that can be used to lookup available options</param>
        /// <param name="workflow">Passes out the workflow object if able to constructed properly</param>
        /// <returns>Returns true if the workflow could be constructed successfully</returns>
        private static bool TryBuildWorkflow(WorkflowDescription workflowDescription,
                                             NodeLibrary nodeLibrary,
                                             [NotNullWhen(true)] out Data.Workflow? workflow)
        {
            // We need to iterate through and build out the graphs that are defined in the workflow description
            bool success = true;
            Dictionary<string, Graph> graphs = new(workflowDescription.Graphs?.Length ?? 0);
            for (int i = 0; i < workflowDescription.Graphs?.Length; ++i)
            {
                // Try to build the graph object so it can be executed
                var graphDescription = workflowDescription.Graphs[i];
                if (TryBuildGraph(graphDescription, nodeLibrary, out var graph) == false)
                {
                    Logger.Error($"[{nameof(PipelineBuilder)}] Failed to build the graph at index {i} '{graphDescription}' for the workflow description '{workflowDescription}'");
                    success = false;
                    continue;
                }

                // Check if there is already a graph with the same id in the collection
                if (graphs.TryGetValue(graph.Id, out var existing) == true)
                {
                    Logger.Error($"[{nameof(PipelineBuilder)}] Failed to build the graph at index {i} '{graphDescription}' for the workflow description '{workflowDescription}', the graph id '{graph.Id}' is already in use by another graph '{existing}'");
                    success = false;
                    continue;
                }
                graphs[graph.Id] = graph;
            }

            // Check that there are graphs in the collection
            if (graphs.Count == 0)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the workflow, there are no graphs contained within the workflow");
                success = false;
            }
            else if (string.IsNullOrWhiteSpace(workflowDescription.EntryGraphId) == false &&
                graphs.ContainsKey(workflowDescription.EntryGraphId) == false)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the workflow, the specified {nameof(WorkflowDescription.EntryGraphId)} '{workflowDescription.EntryGraphId}' couldn't be found");
                success = false;
            }

            // If we failed on any of the elements then we can't build the workflow
            if (success == false)
            {
                workflow = null;
                return false;
            }

            // Create the workflow object that can be used for processing
            workflow = new Data.Workflow(workflowDescription.EntryGraphId, graphs);
            return true;
        }

        /// <summary>
        /// Try to build out the graph from the specified description
        /// </summary>
        /// <param name="graphDescription">The description of the graph that is being built</param>
        /// <param name="nodeLibrary">The library of nodes that can be used to lookup available options</param>
        /// <param name="graph">Passes out the graph object that can be used to process a unit of work</param>
        /// <returns>Returns true if the graph could be constructed successfully</returns>
        private static bool TryBuildGraph(GraphDescription graphDescription,
                                          NodeLibrary nodeLibrary,
                                          [NotNullWhen(true)] out Graph? graph)
        {
            // There are some basic elements that can be grabbed straight from the description
            bool success = true;
            if (string.IsNullOrWhiteSpace(graphDescription.Id) == true)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Invalid graph description '{graphDescription}', no {nameof(GraphDescription.Id)} is defined");
                success = false;
            }
            if (string.IsNullOrWhiteSpace(graphDescription.EntryNodeId) == true)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Invalid graph description '{graphDescription}', no {nameof(GraphDescription.EntryNodeId)} is defined");
                success = false;
            }

            // Find the required properties that are needed for processing
            string[] requiredProperties = graphDescription.RequiredProperties
                ?.Distinct().ToArray() ??
                Array.Empty<string>();

            // Find the nodes that can be used for processing and ensure that they are valid
            Dictionary<string, NodeInstance> nodes = new(graphDescription.Nodes?.Length ?? 0);
            for (int i = 0; i < graphDescription.Nodes?.Length; ++i)
            {
                // The node needs to have a valid and unique id assigned to it for processing
                var node = graphDescription.Nodes[i];
                if (string.IsNullOrWhiteSpace(node.Id) == true)
                {
                    Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the node at index {i} '{node}', there is no {nameof(NodeDescription.Id)} assigned");
                    success = false;
                    continue;
                }
                if (nodes.TryGetValue(node.Id, out var existingNode) == true)
                {
                    Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the node '{node}' at index {i}. The Id '{node.Id}' is already in use by '{existingNode}'");
                    success = false;
                    continue;
                }

                // We need to know what type of node is meant to be used
                if (string.IsNullOrWhiteSpace(node.TypeId) == true)
                {
                    Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the node '{node}' at index {i}, there is no {nameof(NodeDescription.TypeId)} assigned");
                    success = false;
                    continue;
                }
                if (nodeLibrary.TryGetInstanceOfNode(node.TypeId, out var nodeInstance) == false)
                {
                    Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the node '{node}' at index {i}, the {nameof(NodeDescription.TypeId)} value '{node.TypeId}' is not valid for use with the available node definitions");
                    success = false;
                    continue;
                }

                // We can add this entry to the graph for processing
                nodes[node.Id] = new NodeInstance(nodeInstance, node);
            }

            // Check that the entry point exists in the collection
            if (nodes.Count == 0)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the graph description '{graphDescription}', there are no nodes available for processing");
                success = false;
            }
            else if (string.IsNullOrWhiteSpace(graphDescription.EntryNodeId) == false &&
                nodes.ContainsKey(graphDescription.EntryNodeId) == false)
            {
                Logger.Error($"[{nameof(PipelineBuilder)}] Failed when parsing the graph description '{graphDescription}', the entry node id '{graphDescription.EntryNodeId}' does not exist in the collection of nodes");
                success = false;
            }

            // If we failed on any of the elements, then we can't build the graph
            if (success == false)
            {
                graph = null;
                return false;
            }

            // Otherwise, we can build the graph for use
            graph = new Graph(graphDescription, graphDescription.EntryNodeId!, requiredProperties, nodes);
            return true;
        }
    }
}
