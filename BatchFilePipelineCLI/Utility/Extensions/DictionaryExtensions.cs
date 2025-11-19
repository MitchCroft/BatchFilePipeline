namespace BatchFilePipelineCLI.Utility.Extensions
{
    /// <summary>
    /// Provide additional functionality for <see cref="IDictionary{TKey, TValue}"/> values
    /// </summary>
    internal static class DictionaryExtensions
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create a combined dictionary with each subsequent dictionary building on the previous
        /// </summary>
        /// <param name="dictionary">The base dictionary that will be built on top of</param>
        /// <param name="additionals">The additional dictionaries that will be merged into the base</param>
        /// <returns>Returns a new dictionary instance that can be used for processing</returns>
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                                    params IDictionary<TKey, TValue>[] additionals)
        {
            var returnDictionary = new Dictionary<TKey, TValue>(dictionary);
            for (int i = 0; i < additionals?.Length; ++i)
            {
                foreach (var (key, value) in additionals[i])
                {
                    returnDictionary[key] = value;
                }
            }
            return returnDictionary;
        }
    }
}
