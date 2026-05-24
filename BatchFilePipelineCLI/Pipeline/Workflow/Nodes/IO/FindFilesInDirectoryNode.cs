using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.IO
{
    /// <summary>
    /// Define a node that can be used to search through directories for files that should be returned for processing
    /// </summary>
    [PipelineNode(nameof(FindFilesInDirectoryNode), NodeUsage.Identification)]
    internal sealed class FindFilesInDirectoryNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need to get different pieces of information to be able to perform the search operation
        /// </summary>
        private readonly Property _searchDirectoryProperty = Property.Create
        (
            "SearchDirectory",
            "The root directory that should be searched when looking for possible files",
            typeof(string),
            "Parent/Directory/"
        );
        private readonly Property _searchPatternProperty = Property.Create
        (
            "SearchPattern",
            "The pattern that will be used when searching to identify files within the directory that should be returned",
            defaultValue: "*",
            example: "Search patterns like '*.mp4' to retrieve all mp4 files in the directory"
        );
        private readonly Property _searchOptionProperty = Property.Create
        (
            "SearchOption",
            "The method that should be used to search for files in the specified directory",
            defaultValue: SearchOption.AllDirectories
        );

        /// <summary>
        /// This node is going to pass out a collection of files that were identified for use
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The resulting collection of file paths to the elements that were identified for use",
            typeof(string[]),
            example: "[ \"Parent/Directory/File1.txt\", \"Parent/Directory/File2.txt\" ]"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [ _searchDirectoryProperty, _searchPatternProperty, _searchOptionProperty ];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [ _outputProperty ];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                 CancellationToken cancellationToken)
        {
            // Retrieve the properties that will be processed
            string searchDirectory = context.GetInput<string>(_searchDirectoryProperty);
            string searchPattern = context.GetInput<string>(_searchPatternProperty);
            SearchOption searchOption = context.GetInput<SearchOption>(_searchOptionProperty);

            // Find out how many filters there are that need processing
            string[] filterSegments = searchPattern.Split('|', StringSplitOptions.RemoveEmptyEntries);

            // Find the collection of files that need to be handled
            return ValueTask.FromResult(new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, Enumerable.Range(0, filterSegments.Length).SelectMany(i => Directory.EnumerateFiles(searchDirectory, filterSegments[i], searchOption)) }
                }
            ));
        }
    }
}
