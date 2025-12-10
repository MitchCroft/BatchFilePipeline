using System.Collections;

namespace BatchFilePipelineCLI.Utility.Extensions
{
    /// <summary>
    /// Provide additional functionality for <see cref="IEnumerable"/>
    /// </summary>
    public static class IEnumerableExtensions
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Resolve the values within the supplied collection to a realised container
        /// </summary>
        /// <param name="collection">The collection of values to be resolved</param>
        /// <returns>Returns a list of the different elements that can be used</returns>
        public static List<object> ToList(this IEnumerable collection)
        {
            List<object> result = new List<object>();
            foreach (var item in collection)
            {
                result.Add(item);
            }
            return result;
        }
    }
}
