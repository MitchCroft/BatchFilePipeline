using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

using BatchFilePipelineCLI.Logging;

namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// Define the overview of the pipeline that is to be used for processing
    /// </summary>
    [XmlRoot("Pipeline")]
    public sealed class PipelineDescription
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The general name that is given to this pipeline for processing
        /// </summary>
        [XmlAttribute(nameof(Name))]
        public string? Name { get; set; } = null;

        /// <summary>
        /// Define an additional layer of pipeline environment properties that can be applied to the process
        /// </summary>
        [XmlElement("Environment")]
        public KeyValueSection Environment { get; set; } = new();

        /// <summary>
        /// The workflow description that defines the different stages that should be run when processing files
        /// </summary>
        [XmlElement("Workflow")]
        public WorkflowDescription Workflow { get; set; } = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Export the current description
        /// </summary>
        /// <param name="filePath">The file path where the information should be written to</param>
        public void Export(string filePath)
        {
            var serialiser = new XmlSerializer(typeof(PipelineDescription));
            using var fStream = new StreamWriter(filePath); ;
            serialiser.Serialize(fStream, this);
        }

        /// <summary>
        /// Export the current description
        /// </summary>
        /// <param name="stream">The stream where the serialised data should be stored</param>
        public void Export(Stream stream)
        {
            var serialiser = new XmlSerializer(typeof(PipelineDescription));
            serialiser.Serialize(stream, this);
        }

        /// <summary>
        /// Try to read the pipeline description from the specified file
        /// </summary>
        /// <param name="filePath">The file path where the description is located for use</param>
        /// <param name="description">Passes out the description</param>
        /// <returns>Returns true if the description object was able to be read from the file</returns>
        public static bool TryOpen(string filePath, [NotNullWhen(true)] out PipelineDescription? description)
        {
            var serialiser = new XmlSerializer(typeof(PipelineDescription));
            try
            {
                using var fStream = new StreamReader(filePath);
                description = serialiser.Deserialize(fStream) as PipelineDescription;
                return description is not null;
            }
            catch (Exception ex)
            {
                Logger.Exception($"Failed to read the {nameof(PipelineDescription)} from '{filePath}'", ex);
                description = null;
                return false;
            }
        }

        /// <summary>
        /// Try to read the pipeline description from the specified stream
        /// </summary>
        /// <param name="input">The input source of data that is to be read from when retrieving the description</param>
        /// <param name="description">Passes out the description</param>
        /// <returns>Returns true if the description object was able to be read from the stream</returns>
        public static bool TryOpen(Stream input, [NotNullWhen(true)] out PipelineDescription? description)
        {
            var serialiser = new XmlSerializer(typeof(PipelineDescription));
            try
            {
                description = serialiser.Deserialize(input) as PipelineDescription;
                return description is not null;
            }
            catch (Exception ex)
            {
                Logger.Exception($"Failed to read the {nameof(PipelineDescription)} from stream", ex);
                description = null;
                return false;
            }
        }
    }
}
