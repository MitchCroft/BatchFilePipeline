using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections;
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

    /// <summary>
    /// The collection of node definitions that can be used when processing a workflow
    /// </summary>
    public sealed class WorkflowDescription
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The graph elements that will be processed *once* before the main process is run for all file entries collected
        /// </summary>
        [XmlElement("PreProcessGraph")]
        public NodeDescription[] PreProcessGraph { get; set; } = Array.Empty<NodeDescription>();

        /// <summary>
        /// The collection of graph elements that will be run over all identified files
        /// </summary>
        [XmlElement("ProcessGraph")]
        public NodeDescription[] ProcessGraph { get; set;} = Array.Empty<NodeDescription>();

        /// <summary>
        /// The graph elements that will be processed *once* after the main process is run for all file entries collected
        /// </summary>
        [XmlElement("PostProcessGraph")]
        public NodeDescription[] PostProcessGraph { get; set; } = Array.Empty<NodeDescription>();
    }

    /// <summary>
    /// Describe a Node that can be used within the processing workflow and how it should operate
    /// </summary>
    public sealed class NodeDescription
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The human-readable name of the node stage that is being used
        /// </summary>
        /// <remarks>
        /// This is intended to be able to distinguish the different stages defined in a workflow
        /// </remarks>
        [XmlAttribute("Name")]
        public string? Name { get; set; } = null;

        /// <summary>
        /// The unique ID of the node stage
        /// </summary>
        /// <remarks>
        /// This is intended to give a connection to be used in the graph traversal setup
        /// </remarks>
        [XmlAttribute("ID")]
        public string? ID { get; set; } = null;

        /// <summary>
        /// The unique ID of the type of node that is to be created for this stage
        /// </summary>
        /// <remarks>
        /// Each type of node that can be used will have a unique ID that can be used to create specifics
        /// </remarks>
        [XmlAttribute("Type")]
        public string? Type { get; set; } = null;

        /// <summary>
        /// The collection of input values that are to be retrieved for the node to process
        /// </summary>
        [XmlElement("Inputs")]
        public KeyValueSection Inputs { get; set; } = new();

        /// <summary>
        /// The collection of output values that can be mapped for use in the later stages of processing
        /// </summary>
        [XmlElement("Outputs")]
        public KeyValueSection Outputs { get; set; } = new();

        /// <summary>
        /// The collection of connections that can be used as the next stages for processing
        /// </summary>
        [XmlElement("Connections")]
        public KeyValueSection Connections { get; set;} = new();
    }

    /// <summary>
    /// Handle the generic mapping of properties that can be used for processing a workflow pipeline
    /// </summary>
    public sealed class KeyValueSection : IDictionary<string, string?>, IXmlSerializable
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Store the internal collection of values that will be recorded
        /// </summary>
        private readonly Dictionary<string, string?> _values = new();

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the value that is stored under the specified value
        /// </summary>
        public string? this[string key]
        {
            get => ((IDictionary<string, string?>)_values)[key];
            set => ((IDictionary<string, string?>)_values)[key] = value;
        }

        /// <summary>
        /// Retrieve the collection of key values that are contained within the section
        /// </summary>
        public ICollection<string> Keys => ((IDictionary<string, string?>)_values).Keys;

        /// <summary>
        /// Retrieve the collection of values that are contained within the section
        /// </summary>
        public ICollection<string?> Values => ((IDictionary<string, string?>)_values).Values;

        /// <summary>
        /// Retrieve how many elements are stored within the collection
        /// </summary>
        public int Count => ((ICollection<KeyValuePair<string, string?>>)_values).Count;

        /// <summary>
        /// Get the read only flag of the dictionary values
        /// </summary>
        public bool IsReadOnly => ((ICollection<KeyValuePair<string, string?>>)_values).IsReadOnly;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Add a new value to the collection for use
        /// </summary>
        /// <param name="key">The key that the new value should be stored under</param>
        /// <param name="value">The value that is to be stored within the collection</param>
        public void Add(string key, string? value)
        {
            ((IDictionary<string, string?>)_values).Add(key, value);
        }

        /// <summary>
        /// Add a key-value pair to the collection for use
        /// </summary>
        /// <param name="item">The pair of values that are to be added to the collection</param>
        public void Add(KeyValuePair<string, string?> item)
        {
            ((ICollection<KeyValuePair<string, string?>>)_values).Add(item);
        }

        /// <summary>
        /// Clear all of the values that are stored within the collection
        /// </summary>
        public void Clear()
        {
            ((ICollection<KeyValuePair<string, string?>>)_values).Clear();
        }

        /// <summary>
        /// Check if the internal collection contains the specified pair of values
        /// </summary>
        /// <param name="item">The item that is being checked for</param>
        /// <returns>Returns true if the collection contains the values</returns>
        public bool Contains(KeyValuePair<string, string?> item)
        {
            return ((ICollection<KeyValuePair<string, string?>>)_values).Contains(item);
        }

        /// <summary>
        /// Check if there is a key value within the current collection
        /// </summary>
        /// <param name="key">The key that is being checked</param>
        /// <returns>Returns true if there is a specific key in the collection</returns>
        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, string?>)_values).ContainsKey(key);
        }

        /// <summary>
        /// Copy the internal collection of valeus into a buffer
        /// </summary>
        /// <param name="array">The buffer that is to be filled with the data</param>
        /// <param name="arrayIndex">The index of the in the buffer to start filling from</param>
        public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, string?>>)_values).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Retrieve the enumerator for the collection of values that are needed
        /// </summary>
        /// <returns>Returns an enumerable that can be processed</returns>
        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string?>>)_values).GetEnumerator();
        }

        /// <summary>
        /// Remove the entry with the specified key
        /// </summary>
        /// <param name="key">The key that is to be removed from the collection</param>
        /// <returns>Returns true if there was an element removed</returns>
        public bool Remove(string key)
        {
            return ((IDictionary<string, string?>)_values).Remove(key);
        }

        /// <summary>
        /// Remove the key-value pair element from the collection
        /// </summary>
        /// <param name="item">The item that is to be removed from the collection</param>
        /// <returns>Returns true if an element was removed</returns>
        public bool Remove(KeyValuePair<string, string?> item)
        {
            return ((ICollection<KeyValuePair<string, string?>>)_values).Remove(item);
        }

        /// <summary>
        /// Try to get the value stored under the specified key
        /// </summary>
        /// <param name="key">The key that is being searched for</param>
        /// <param name="value">Passes out the value stored under the key value</param>
        /// <returns>Returns true if was able to retrieve the value for use</returns>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string? value)
        {
            return ((IDictionary<string, string?>)_values).TryGetValue(key, out value);
        }

        //INTERFACE

        /// <summary>
        /// Retrieve the generic enumerator for iteration
        /// </summary>
        /// <returns>Returns a generic enumerator for iteration</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_values).GetEnumerator();
        }

        /// <summary>
        /// Retrieve the specific schema that will be used for processing
        /// </summary>
        /// <returns>Always returns null</returns>
        XmlSchema? IXmlSerializable.GetSchema() => null;

        /// <summary>
        /// Read in the element values from the XML source
        /// </summary>
        /// <param name="reader">The reader object that is processing the data</param>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            // We don't want any old data polluting the entries
            _values.Clear();

            // Consume the opening tag element
            reader.ReadStartElement();

            // Read in all of the values that are required
            while (reader.NodeType == XmlNodeType.Element)
            {
                string key = reader.Name;
                string value = reader.ReadElementContentAsString();
                _values[key] = value;
            }

            // Consume the closing tag element
            reader.ReadEndElement();
        }

        /// <summary>
        /// Write out the element values to the XML document
        /// </summary>
        /// <param name="writer">The writer that will be used to store the data being processed</param>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            foreach (var (key, value) in _values)
            {
                writer.WriteElementString(key, value);
            }
        }
    }
}
