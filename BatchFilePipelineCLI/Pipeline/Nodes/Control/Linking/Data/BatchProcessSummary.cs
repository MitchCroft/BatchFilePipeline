using BatchFilePipelineCLI.Pipeline.Data;

namespace BatchFilePipelineCLI.Pipeline.Nodes.Control.Linking.Data
{
    /// <summary>
    /// A summary object that contains a collection of information about how a batch process was handled
    /// </summary>
    public readonly struct BatchProcessSummary
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The id of the pipeline where the graph was located that was processed
        /// </summary>
        public readonly PipelineId Pipeline;

        /// <summary>
        /// The id of the graph that was executed as a part of the batch process
        /// </summary>
        public readonly string GraphId;

        /// <summary>
        /// The result code for the operation
        /// </summary>
        public readonly int ResultCode;

        /// <summary>
        /// Details about the process that was performed
        /// </summary>
        public readonly string? Details;

        /// <summary>
        /// The length of time that the main process was running for
        /// </summary>
        public readonly TimeSpan Duration;

        /// <summary>
        /// The collection of identifiers that were processed successfully in the run
        /// </summary>
        public readonly object?[] Successful;

        /// <summary>
        /// The collection of identifiers that failed to process successfully in the run
        /// </summary>
        public readonly object?[] Failed;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Basic flag that indicates if the operation was completed successfully
        /// </summary>
        public readonly bool WasSuccessful => ResultCode == 0;

        /// <summary>
        /// The total number of files that were processed in pipeline
        /// </summary>
        public readonly int Total => Successful.Length + Failed.Length;

        /// <summary>
        /// The success rate for the elements that were processed in the pipeline
        /// </summary>
        public readonly float SuccessRate => Successful.Length / (float)Total;

        /// <summary>
        /// The failure rate for the elements that were processed in the pipeline
        /// </summary>
        public readonly float FailRate => Failed.Length / (float)Total;

        /// <summary>
        /// Property access to the string representation of the report
        /// </summary>
        public readonly string Report => GetSummaryReport();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the process summary with the required information
        /// </summary>
        /// <param name="resultCode">The result code for the operation</param>
        /// <param name="details">Details about the process that was performed</param>
        /// <param name="duration">The length of time that the main process was running for</param>
        /// <param name="successful">The collection of identifiers that were processed successfully in the run</param>
        /// <param name="failed">The collection of identifiers that failed to process successfully in the run</param>
        public BatchProcessSummary(PipelineId pipeline,
                                   string graphId,
                                   int resultCode,
                                   string? details,
                                   TimeSpan duration,
                                   object?[] successful,
                                   object?[] failed)
        {
            Pipeline = pipeline;
            GraphId = graphId;
            ResultCode = resultCode;
            Details = details;
            Duration = duration;
            Successful = successful;
            Failed = failed;
        }

        /// <summary>
        /// Create a string that will summarise the information contained in the object
        /// </summary>
        public string GetSummaryReport()
        {
            return $@"==================== RUN SUMMARY ====================
Result = {(WasSuccessful ? "Success" : $"Error ({ResultCode})")}{(string.IsNullOrWhiteSpace(Details) ? string.Empty : $"\nDetails = {Details}")}
Duration = {Duration}

GraphId = {GraphId}
Pipeline = {Pipeline}

Processed Count = {Total}
Success Rate = {SuccessRate:P}
Failure Rate = {FailRate:P}

Successful {Successful.Length}/{Total}:
	{string.Join("\n\t", Successful)}

Failed {Failed.Length}/{Total}:
	{string.Join("\n\t", Failed)}
==================== END SUMMARY ====================";
        }

        /// <summary>
        /// Use the summary report as the representation of this object
        /// </summary>
        public override string ToString() => GetSummaryReport();
    }
}
