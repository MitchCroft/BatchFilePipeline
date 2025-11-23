using BatchFilePipelineCLI.DynamicProperties;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Identification
{
    /// <summary>
    /// Define a node that can be used to search through directories for files that should be returned for processing
    /// </summary>
    [PipelineNode(nameof(FindFilesInDirectoryNode), NodeUsage.Identification)]
    internal class FindFilesInDirectoryNode : IPipelineNode
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
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(IReadOnlyDictionary<string, object?> inputs,
                                                                 CancellationToken cancellationToken)
        {
            // Process the search operation to find the files that are needed
            try
            {
                string searchDirectory = (string)inputs[_searchDirectoryProperty.Name]!;
                string searchPattern = (string)inputs[_searchPatternProperty.Name]!;
                SearchOption searchOption = (SearchOption)inputs[_searchOptionProperty.Name]!;
                return ValueTask.FromResult(new ExecutionResult
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, Directory.GetFiles(searchDirectory, searchPattern, searchOption) }
                    }
                ));
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return ValueTask.FromResult(new ExecutionResult(ex)); }
        }
    }
}
