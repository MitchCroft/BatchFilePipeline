using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// Handle the generic mapping of properties that can be used for processing a workflow pipeline
    /// </summary>
    public sealed class KeyValueSection : Dictionary<string, string>, IXmlSerializable
    {
        /*----------Functions----------*/
        //INTERFACE

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
            Clear();

            // Consume the opening tag element
            reader.ReadStartElement();

            // Read in all of the values that are required
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    string key = reader.Name;
                    string value = reader.ReadElementContentAsString();
                    this[key] = value;
                } else
                {
                    // Skip anything that's not an element (comments, whitespace, etc.)
                    reader.Read();
                }
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
            foreach (var (key, value) in this)
            {
                writer.WriteElementString(key, value);
            }
        }
    }
}
