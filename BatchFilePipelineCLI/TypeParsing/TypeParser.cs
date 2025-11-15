using BatchFilePipelineCLI.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.TypeParsing
{
    /// <summary>
    /// Handle the parsing of the generic input values into the types required to operate the pipeline
    /// </summary>
    internal static class TypeParser
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The collection of processors that are available to handle parsing operations
        /// </summary>
        private static readonly List<ITypeParserProcessor> _processors = new List<ITypeParserProcessor>();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Function that will be used to find all of the available processors within the loaded assemblies
        /// </summary>
        public static void FindProcessors()
        {
            // Clear any previous entries that were found
            _processors.Clear();

            // Search all of the loaded assemblies for processors
            _processors.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ITypeParserProcessor).IsAssignableFrom(type) &&
                               type.IsInterface == false &&
                               type.IsAbstract == false)
                .Select(type =>
                {
                    try { return Activator.CreateInstance(type) as ITypeParserProcessor; }
                    catch (Exception ex)
                    {
                        Logger.Exception($"Encountered an exception when trying to create an instance of the type '{type}' for processing", ex);
                        return (ITypeParserProcessor?)null;
                    }
                })
                .Where(x => x is not null)
                .OrderByDescending(x => x!.Priority)
                .Select(x => x!));
        }

        /// <summary>
        /// Try to parse the specified input value into the target type
        /// </summary>
        /// <param name="inputValue">The input string value that is to be processed</param>
        /// <param name="targetType">The target type that is to be used for processing</param>
        /// <param name="parsedValue">Passes out the parsed value or null if unable</param>
        /// <returns>Returns true if the parsed value is a valid value that was parsed by a processor</returns>
        public static bool TryParse(string inputValue, Type targetType, [MaybeNull] out object? parsedValue)
        {
            // Look through all of the processors to see if one can handle the request
            foreach (ITypeParserProcessor processor in _processors)
            {
                // Check that this processor can handle the target type
                if (processor.CanProcessOutputType(targetType) == false)
                {
                    continue;
                }

                // Try to parse value into the specified type
                if (processor.TryProcessToType(inputValue, targetType, out parsedValue) == true)
                {
                    return true;
                }
            }
            // No processor could handle the request
            Logger.Error($"No type parser processor could be found to handle parsing to the type '{targetType}'");
            parsedValue = null;
            return false;
        }

        /// <summary>
        /// Try to parse the specified input value into the target type
        /// </summary>
        /// <param name="inputValue">The input string value that is to be processed</param>
        /// <param name="parsedValue">Passes out the parsed value or null if unable</param>
        /// <returns>Returns true if the parsed value is a valid value that was parsed by a processor</returns>
        public static bool TryParse<T>(string inputValue, [MaybeNull] out T? parsedValue)
        {
            if (TryParse(inputValue, typeof(T), out object? value) == true &&
                value is T castValue)
            {
                parsedValue = castValue;
                return true;
            }
            parsedValue = default;
            return false;
        }

        //PRIVATE

        /// <summary>
        /// Find the initial collection of processors when the class is first accessed
        /// </summary>
        static TypeParser() => FindProcessors();
    }
}
