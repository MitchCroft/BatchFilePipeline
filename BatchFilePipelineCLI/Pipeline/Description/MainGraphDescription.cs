using System.Xml.Serialization;

namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// Define the collection of values that will be used to process the main section of pipeline process
    /// </summary>
    public sealed class MainGraphDescription : IGraphDescription
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Define an additional layer of pipeline environment properties that can be applied to the process
        /// </summary>
        [XmlElement("Environment")]
        public KeyValueSection Environment { get; set; } = new();

        /// <summary>
        /// The graph elements that will be run to process the identification of different files that should be processed
        /// </summary>
        public GraphDescription IdentificationGraph { get; set; } = new GraphDescription();

        /// <summary>
        /// The graph elements that will be run to process the individual files that were identified as a part of the former stage
        /// </summary>
        public GraphDescription ProcessGraph { get; set; } = new GraphDescription();
    }
}
