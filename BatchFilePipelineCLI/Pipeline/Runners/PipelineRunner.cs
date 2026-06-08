using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Builders;
using BatchFilePipelineCLI.Pipeline.Data;
using BatchFilePipelineCLI.Pipeline.Description;
using BatchFilePipelineCLI.Pipeline.Nodes.Control;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes;
using BatchFilePipelineCLI.PropertyResolver;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Pipeline.Runners
{
    /// <summary>
    /// Manage the result of the loaded pipeline that can be executed to perform the required work
    /// </summary>
    public sealed class PipelineRunner : IPipelineRunner
    {
        /*----------Variables----------*/
        //CONST

        /// <summary>
        /// The expected key that will be used to identify the next node that is to be processed
        /// </summary>
        private const string DEFAULT_NEXT_NODE_KEY = "Default";

        //PRIVATE

        /// <summary>
        /// The cached collection of pipelines that have been loaded for processing runtime functionality
        /// </summary>
        private readonly Dictionary<PipelineId, Data.Pipeline?> _pipelines = new(1);

        /// <summary>
        /// The lookup collection of nodes that can be used to build out the pipelines
        /// </summary>
        private readonly NodeLibrary _nodeLibrary;

        /// <summary>
        /// The collection of environment variables that exist by default for the processing of the pipeline
        /// </summary>
        private readonly IReadOnlyDictionary<string, string> _environmentVariables;

        /// <summary>
        /// The collection of argument variables that have been provided to the application for processing
        /// </summary>
        private readonly IReadOnlyDictionary<string, string> _argumentVariables;

        /// <summary>
        /// Define a property that can be used to identify the maximum traversal depth for the graph of elements
        /// </summary>
        private readonly Property _maxTraversalDepthProperty = Property.Create
        (
            "MaxTraversalDepth",
            "The maximum number of node steps that can be made when processing a graph before the process is killed",
            defaultValue: 25
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the runner object that will be used to process the actual running of the required logic
        /// </summary>
        public PipelineRunner(NodeLibrary nodeLibrary,
                              IReadOnlyDictionary<string, string> environmentVariables,
                              IReadOnlyDictionary<string, string> argumentVariables)
        {
            _nodeLibrary = nodeLibrary;
            _environmentVariables = environmentVariables;
            _argumentVariables = argumentVariables;
        }

        /// <summary>
        /// Handle the initial process of running the pipeline with the required values
        /// </summary>
        /// <param name="pipelinePath">The initial location of the pipeline asset that is to be run</param>
        /// <param name="rootPath">The root path that the pipeline path will be relative to for resolving final location</param>
        /// <param name="cancellationToken">Cancellation token that is used to control the lifespan of the running operation</param>
        /// <returns>Returns an error code that describes the success state of the operation, where 0 is success</returns>
        public async ValueTask<int> ExecuteMainAsync(string pipelinePath,
                                                     string rootPath,
                                                     CancellationToken cancellationToken)
        {
            var result = await ((IPipelineRunner)this).ExecuteSubProcessAsync
            (
                new PipelineId(pipelinePath, rootPath),
                new PipelineContext
                {
                    Runner = this,
                    EnvironmentVariables = _environmentVariables,
                    RuntimeVariables = new Dictionary<string, object?>(),
                    InputVariables = new Dictionary<string, object?>(),
                    CancellationToken = cancellationToken,
                },
                entryGraphId: null
            );
            if (result.IsError)
            {
                Logger.Error($"[{nameof(PipelineRunner)}] Encountered an error while processing: {result}");
            }
            else
            {
                Logger.Log($"[{nameof(PipelineRunner)}] Completed processing: {result}");
            }
            return result.ResultCode;
        }

        //INTERFACE
        
        /// <summary>
        /// Handle the running of a specified sub-process within the main execution loop
        /// </summary>
        /// <param name="id">The id of the pipeline asset that is to be run</param>
        /// <param name="context">The context for the currently executing operation</param>
        /// <param name="entryGraphId">[Optional] The id if the graph within the target pipeline that is to be run</param>
        /// <returns>Returns the result object from the running of the process</returns>
        async ValueTask<ExecutionResult> IPipelineRunner.ExecuteSubProcessAsync(PipelineId id,
                                                                                PipelineContext context,
                                                                                string? entryGraphId)
        {
            // Look for the pipeline that is intended to be run
            if (TryGetPipeline(id, out var pipeline) == false)
            {
                return new ExecutionResult
                (
                    404,
                    $"Unable to find the pipeline asset '{id}' to run the process"
                );
            }

            // Check that we have the target graph that will be executed
            if (string.IsNullOrWhiteSpace(entryGraphId) == true &&
                pipeline.Workflow.IsLibrary == true)
            {
                return new ExecutionResult
                (
                    500,
                    $"Unable to run the pipeline asset '{pipeline}', no {nameof(WorkflowDescription.EntryGraphId)} value is defined for the workflow and none was supplied"
                );
            }

            // Check that the target graph is contained
            string entryPoint = string.IsNullOrWhiteSpace(entryGraphId) ? pipeline.Workflow.EntryGraphId! : entryGraphId;
            if (pipeline.Workflow.Graphs.TryGetValue(entryPoint, out var graph) == false)
            {
                return new ExecutionResult
                (
                    404,
                    $"Unable to run the graph '{entryPoint}' from the pipeline asset '{pipeline}', the graph could not be found"
                );
            }

            // Process the running of the graph
            return await ExecuteGraphAsync(pipeline, graph, context);
        }

        //PRIVATE

        /// <summary>
        /// Manage the execution of a graph object to completion while processing the required data
        /// </summary>
        /// <param name="pipeline">The pipeline that that the supplied graph exists on</param>
        /// <param name="graph">The specific graph object that is to be processed</param>
        /// <param name="context">The context for the currently running operation</param>
        /// <returns>Returns the execution results for the graph that was run</returns>
        private async ValueTask<ExecutionResult> ExecuteGraphAsync(Data.Pipeline pipeline,
                                                                   Graph graph,
                                                                   PipelineContext context)
        {
            // How deep can this graph process
            if (Resolver.TryResolveEnvironmentVariable(_maxTraversalDepthProperty, pipeline.EnvironmentVariables, out int maxTraversalDepth) == false)
            {
                return new ExecutionResult
                (
                    500,
                    $"[{nameof(PipelineRunner)}] Failed to resolve the '{_maxTraversalDepthProperty}' property from the environment variables, can not continue"
                );
            }

            // Get the initial node for the graph
            if (graph.Nodes.TryGetValue(graph.EntryNodeId, out var activeNode) == false)
            {
                return new ExecutionResult
                (
                    404,
                    $"[{nameof(PipelineRunner)}] Failed to find the nominated entry point node '{graph.EntryNodeId}' on the graph '{graph}' as a part of the pipeline '{pipeline}'"
                );
            }

            // Log the graph that is being run
            Logger.Log($"[{nameof(PipelineRunner)}] Running the graph '{graph}' from '{pipeline}' with\n\tEnvironment Variables:\n\t\t{string.Join("\n\t\t", pipeline.EnvironmentVariables.Select((v, i) => $"{i}.\t{v.Key}={v.Value}"))}\n\tRuntime Variables:\n\t\t{string.Join("\n\t\t", context.RuntimeVariables.Select((v, i) => $"{i}.\t{v.Key}={v.Value}"))}");

            // Create the objects that will be used to process the graph cycle
            List<string> progression = new(Math.Min(maxTraversalDepth, graph.Nodes.Count));
            Dictionary<string, object?> outputResults = new();
            Dictionary<string, object?> inputBuffer = new();
            Dictionary<string, object?> localScopeRuntime = new(context.RuntimeVariables);

            // Time how long this process takes to operate
            var stopwatch = Stopwatch.StartNew();

            // Step through the graph to process the required data
            int steps = 0;
            for (; steps < maxTraversalDepth; ++steps)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////---------------------------Determine Node Processing----------------------------//////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////

                // Retrieve the elements that are needed for processing
                var (node, description) = activeNode;
                progression.Add(description.ToString());

                // Determine the type of node that we are working with
                ExecutionResult nodeOutput = default;
                switch (node)
                {
                    // Special case nodes that have unique, bespoke functionality on the graph
                    case ExportValuesNode:
                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////----------------------------Resolve Export Variables----------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        
                        // The collection of inputs is defined entriely by the description and will have loose typing
                        foreach (var (name, descriptor) in description.Inputs)
                        {
                            // Try to resolve the descriptor into a value that will be useful
                            if (Resolver.TryResolveLooseDescriptor(descriptor, pipeline.EnvironmentVariables, localScopeRuntime, out var resolvedInput) == false)
                            {
                                return new ExecutionResult
                                (
                                    422,
                                    $"[{nameof(PipelineRunner)}] Node={description} Graph={graph} Pipeline={pipeline} Couldn't resolve the loose descriptor '{descriptor}' for export"
                                );
                            }

                            // Stash the value for export use
                            outputResults[name] = resolvedInput;
                        }
                        break;

                    // Default nodes, anything else will run as normal
                    default:
                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////-------------------------------Create Node Inputs-------------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////

                        // Find the collection of inputs that are needed for the node
                        inputBuffer.Clear();
                        var nodeInputs = node.GetInputProperties();
                        for (int i = 0; i < nodeInputs.Count; ++i)
                        {
                            // Look for a specified descriptor for the input
                            description.Inputs.TryGetValue(nodeInputs[i].Name, out var inputDescriptor);

                            // Try to resolve the description into a value that can be assigned
                            if (Resolver.TryResolveDescriptor(inputDescriptor, nodeInputs[i], pipeline.EnvironmentVariables, localScopeRuntime, out var resolvedInput) == false)
                            {
                                return new ExecutionResult
                                (
                                    422,
                                    $"[{nameof(PipelineRunner)}] Couldn't resolve the descriptor '{inputDescriptor}' for the property '{nodeInputs[i]}' for the node '{description}' on the graph '{graph}' in the pipeline '{pipeline}'"
                                );
                            }

                            // The node can use this value for processing
                            inputBuffer[nodeInputs[i].Name] = resolvedInput;
                        }

                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////----------------------------------Process Node----------------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////

                        // We can process the node operation and receive the outputs that need to be handled
                        try
                        {
                            var nodeContext = new PipelineContext
                            {
                                Runner = context.Runner,
                                EnvironmentVariables = pipeline.EnvironmentVariables,
                                RuntimeVariables = localScopeRuntime,
                                InputVariables = inputBuffer,
                                CancellationToken = context.CancellationToken,
                            };
                            nodeOutput = await node.ProcessNodeResultAsync(nodeContext);
                        }
                        catch (Exception ex)
                        {
                            nodeOutput = new ExecutionResult(new Exception($"[{nameof(PipelineRunner)}] Node={description} Graph={graph} Pipeline={pipeline}", ex));
                        }
                        if (context.CancellationToken.IsCancellationRequested == true)
                        {
                            return new ExecutionResult(new TaskCanceledException($"[{nameof(PipelineRunner)}] Node={description} Graph={graph} Pipeline={pipeline}"));
                        }

                        // If the process failed, we need to stop here
                        if (nodeOutput.IsError == true)
                        {
                            return nodeOutput;
                        }

                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////------------------------------Handle Node Outputs-------------------------------//////////
                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        
                        // If there are results, then we can process them in some way or another
                        if (nodeOutput.Results is not null)
                        {
                            // We have the collection of outputs that need to be mapped into the runtime variables for use
                            var nodeOutputs = node.GetOutputProperties();
                            for (int i = 0; i < nodeOutputs.Count; ++i)
                            {
                                // See if there is a mapping value for the output
                                if (description.Outputs.TryGetValue(nodeOutputs[i].Name, out var outputMapping) == false ||
                                    string.IsNullOrWhiteSpace(outputMapping) == true)
                                {
                                    continue;
                                }

                                // Check if there is an output value in the result
                                if (nodeOutput.Results.TryGetValue(nodeOutputs[i].Name, out var outputValue) == false)
                                {
                                    continue;
                                }

                                // Assign the output to the runtime container
                                localScopeRuntime[outputMapping] = outputValue;
                            }
                        }
                        break;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////-------------------------------Identify Next Node-------------------------------//////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////
                
                // If there are no connections specified, then we're at the end of the branch
                if (description.Connections.Count == 0)
                {
                    break;
                }

                // We need to try and find the node that is to be used next
                string nextNode = nodeOutput.Next ?? DEFAULT_NEXT_NODE_KEY;
                if (description.Connections.TryGetValue(nextNode, out var nextNodeId) == false &&
                    description.Connections.TryGetValue(DEFAULT_NEXT_NODE_KEY, out nextNodeId) == false)
                {
                    return new ExecutionResult
                    (
                        404,
                        $"[{nameof(PipelineRunner)}] Encountered an error while processing Node={description} Graph={graph} Pipeline={pipeline}. Unable to find a matching connection for the selection case '{(nextNode != DEFAULT_NEXT_NODE_KEY ? $"{nextNode}/{DEFAULT_NEXT_NODE_KEY}" : nextNode)}'"
                    );
                }

                // If the next node id is blank, we've also reached the end of the graph (in case switching logic needs to end on a path
                if (string.IsNullOrWhiteSpace(nextNodeId) == true)
                {
                    break;
                }

                // Try to find the node that is to be used next
                if (graph.Nodes.TryGetValue(nextNodeId, out activeNode) == true)
                {
                    continue;
                }

                // We were unable to find the node that is to be used for processing
                return new ExecutionResult
                (
                    404,
                    $"[{nameof(PipelineRunner)}] Encountered an error while processing Node={description} Graph={graph} Pipeline={pipeline}. Selected output path was '{nextNodeId}' but no node in the graph with that id could be found"
                );
            }

            // If we hit the maximum number of steps, nothing we can do
            if (steps == maxTraversalDepth)
            {
                return new ExecutionResult
                (
                    504,
                    $"[{nameof(PipelineRunner)}] Reached the maximum number of steps ({maxTraversalDepth}) while processing Graph={graph} Pipeline={pipeline}"
                );
            }

            // We're good
            stopwatch.Stop();
            return new ExecutionResult
            (
                outputResults,
                additionalDetails: $"[{nameof(PipelineRunner)}] Finished executing Graph={graph} Pipeline={pipeline} after: {stopwatch.Elapsed}{(progression.Count > 0 ? $"\n\t{string.Join("\n\t", progression.Select((v, i) => $"{i}.\t{v}"))}" : string.Empty)}"
            );
        }

        /// <summary>
        /// Try to retrieve the pipeline asset that can be used for processing operations
        /// </summary>
        /// <param name="id">The id of the pipeline that is to be processed</param>
        /// <param name="pipeline">Passes out the pipeline that matches the specified id</param>
        /// <returns>Returns true if the pipeline definition returned is valid for use</returns>
        private bool TryGetPipeline(PipelineId id,
                                    [NotNullWhen(true)] out Data.Pipeline? pipeline)
        {
            // Check if we have a cached answer
            if (_pipelines.TryGetValue(id, out pipeline) == true)
            {
                return pipeline is not null;
            }

            // Try to build a pipeline asset from the disk definition
            bool result = PipelineBuilder.TryBuildPipeline(id, _nodeLibrary, _environmentVariables, _argumentVariables, out pipeline);
            _pipelines[id] = pipeline;
            return result;
        }
    }
}
