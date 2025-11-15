using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.TypeParsing
{
    /// <summary>
    /// Provide the basic skeleton for a parser of a specific type
    /// </summary>
    /// <typeparam name="T">The type of value that will be handled by the processor</typeparam>
    public abstract class TypeParserProcessor<T> : ITypeParserProcessor
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Flags the priority of this processor when multiple processors are available for a type
        /// </summary>
        /// <remarks>
        /// The higher the value the greater the priority. Processors with higher priority will be chosen over those with lower priority.
        /// </remarks>
        public virtual int Priority { get; } = 0;

        /// <summary>
        /// Flags if children of the specified type can be processed by this parser as well
        /// </summary>
        public virtual bool CanProcessChildren { get; } = false;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Check to see if the supplied Type can be processed by this processor
        /// </summary>
        /// <param name="targetType">The type of the expected output value</param>
        /// <returns>Returns true if the output type is supported for use</returns>
        public bool CanProcessOutputType(Type targetType)
        {
            return CanProcessChildren ? 
                typeof(T).IsAssignableFrom(targetType) :
                typeof(T) == targetType;
        }

        /// <summary>
        /// Try to parse the input string value as a specified target type
        /// </summary>
        /// <param name="inputValue">The input string that is to be processed into the recorded value</param>
        /// <param name="targetType">The target type that is to be output</param>
        /// <param name="value">Passes out the parsed value if available, null if not</param>
        /// <returns>Returns true if the parsed value is valid for use</returns>
        public bool TryProcessToType(in string inputValue, Type targetType, [MaybeNull] out object? value)
        {
            if (TryParseToValue(inputValue, targetType, out T? typedValue) == false)
            {
                value = null;
                return false;
            }
            value = typedValue;
            return true;
        }

        //PROTECTED

        /// <summary>
        /// Try to parse the input string value as a specified target type
        /// </summary>
        /// <param name="inputValue">The input string that is to be processed into the recorded value</param>
        /// <param name="targetType">The target type that is to be output</param>
        /// <param name="value">Passes out the parsed value if available, null if not</param>
        /// <returns>Returns true if the parsed value is valid for use</returns>
        protected abstract bool TryParseToValue(in string inputValue, Type targetType, [MaybeNull] out T? value);
    }
}
