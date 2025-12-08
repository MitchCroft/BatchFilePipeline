using BatchFilePipelineCLI.Utility.Data;

namespace BatchFilePipelineCLI.PropertyResolver
{
    /// <summary>
    /// Cache information that will be used when resolving a requested input value
    /// </summary>
    internal struct ResolverToken
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// Flags if this token represents a basic value or a dynamic variable
        /// </summary>
        public bool IsVariable;

        /// <summary>
        /// The collection of values that have been assigned to the token for evaluation
        /// </summary>
        public DynamicDataBufferInstance<string>? Values;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Helper flag to indicate if the token has any real value that can be processed
        /// </summary>
        public bool IsEmpty => (Values?.Count ?? 0) == 0;
    }
}
