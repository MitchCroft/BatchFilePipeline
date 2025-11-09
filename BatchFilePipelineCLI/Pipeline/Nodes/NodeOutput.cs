namespace BatchFilePipelineCLI.Pipeline.Nodes
{
    /// <summary>
    /// Define the result of a Node being run and the output of the process
    /// </summary>
    public readonly struct NodeOutput
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// A result code that can be used to indicate the state of the process that was run
        /// </summary>
        /// <remarks>
        /// A value of 0 will be success, anything else is a failure
        /// </remarks>
        public readonly int ResultCode;

        /// <summary>
        /// Additional information about the result that is returned
        /// </summary>
        public readonly string DetailMessage;

        /// <summary>
        /// The collection of output results from the node processing when successful
        /// </summary>
        public readonly IDictionary<string, object?>? Results;

        /// <summary>
        /// For Nodes with split paths, the name of the connection to be run next
        /// </summary>
        /// <remarks>
        /// For example, an Evaluation Node would return True or False depending on the condition
        /// </remarks>
        public readonly string? NextNode;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Flags if there was an error while processing the result
        /// </summary>
        public readonly bool IsError => ResultCode != 0;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create a successful Node Output result object
        /// </summary>
        /// <param name="results">The resulting information that came from the Node</param>
        /// <param name="nextNode">[Optional] For Nodes with split paths, the name of the connection to be run next</param>
        /// <param name="additionalDetails">[Optional] Additional information about the process that was run for processing</param>
        public NodeOutput(IDictionary<string, object?> results, string? nextNode = null, string? additionalDetails = null)
        {
            ResultCode = 0;
            DetailMessage = additionalDetails ?? string.Empty;
            Results = results;
            NextNode = nextNode;
        }

        /// <summary>
        /// Create the Node Output result from an exception that occurred
        /// </summary>
        /// <param name="exception">The exception that resulted during processing</param>
        public NodeOutput(Exception exception) :
            this(exception.HResult, exception.ToString())
        {}

        /// <summary>
        /// Create the Node Output with an error response setup
        /// </summary>
        /// <param name="resultCode">The result code that will be used to represent the result of the Node</param>
        /// <param name="errorMessage">An additional error message that provides additional information</param>
        /// <exception cref="ArgumentException">Exception will be thrown if the successful return code is used</exception>
        public NodeOutput(int resultCode, string errorMessage)
        {
            if (resultCode == 0)
            {
                throw new ArgumentException($"Using successful result code '0' in error response constructor!");
            }
            ResultCode = resultCode;
            DetailMessage = errorMessage;
            Results = null;
            NextNode = null;
        }

        /// <summary>
        /// Retrieve a string representation of the result
        /// </summary>
        public override string ToString()
        {
            return IsError ? $"[Failure: {ResultCode}] {DetailMessage}" : $"[Success]{(string.IsNullOrWhiteSpace(DetailMessage) == true ? string.Empty : $" {DetailMessage}")}";
        }
    }
}
