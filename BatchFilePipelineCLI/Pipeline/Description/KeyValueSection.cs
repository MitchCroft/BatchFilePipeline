using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Pipeline.Description
{
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
