using BatchFilePipelineCLI.DynamicProperties;
using BatchFilePipelineCLI.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// A manager wrapper that can be used to run the functionality required for a graph at runtime
    /// </summary>
    internal sealed class GraphRunner
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The graph object that will be run via this process
        /// </summary>
        private readonly Graph _graph;

        /// <summary>
        /// The collection of environment variables that will be used when processing
        /// </summary>
        private readonly IDictionary<string, string?> _environmentVariables;

        /// <summary>
        /// Define a property that can be used to identify the maximum traversal depth for the graph of elements
        /// </summary>
        private readonly Property _maxTraversalDepthProperty = Property.Create
        (
            "maxTraversalDepth",
            "The maximum number of node steps that can be made when processing a graph before the process is killed",
            defaultValue: 25
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the graph runner with the required values for processing
        /// </summary>
        /// <param name="graph">The graph description that describes the process that is to occurr</param>
        /// <param name="environmentVariables">The collection of environment variables that should be used for execution</param>
        public GraphRunner(Graph graph,
                           IDictionary<string, string?> environmentVariables)
        {
            _graph = graph;
            _environmentVariables = environmentVariables;
        }

        /// <summary>
        /// Run the contained graph with the specified information and return the output that is resolved
        /// </summary>
        /// <param name="runtimeVariables">The collection of existing runtime variables that are available for use</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the result of the execution process</returns>
        public async ValueTask<ExecutionResult> ExecuteGraphAsync(IDictionary<string, object?> runtimeVariables,
                                                                  CancellationToken cancellationToken)
        {
            // Try to resolve the traversal depth that will be used when running this graph
            if (ArgumentResolver.TryResolveEnvironmentVariable(_maxTraversalDepthProperty, _environmentVariables, out int maxTraversalDepth) == false)
            {
                return new ExecutionResult
                (
                    500,
                    $"[{nameof(GraphRunner)}] Failed to resolve the '{_maxTraversalDepthProperty}' property from the environemnt variables, cannot continue"
                );
            }

            // Process the graph result
            return await _graph.ExecuteGraphAsync(_environmentVariables, runtimeVariables, maxTraversalDepth, cancellationToken);
        }
    }
}
