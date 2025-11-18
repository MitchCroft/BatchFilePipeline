using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Utility.Preserve;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.TypeParsing.Parsers
{
    /// <summary>
    /// Handle the conversions for all Enum types
    /// </summary>
    [Preserve]
    internal sealed class EnumTypeParserProcessor : ITypeParserProcessor
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Flags the priority of this processor when multiple processors are available for a type
        /// </summary>
        /// <remarks>
        /// The higher the value the greater the priority. Processors with higher priority will be chosen over those with lower priority.
        /// </remarks>
        public int Priority { get; } = int.MinValue + 1;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Check to see if the supplied Type can be processed by this processor
        /// </summary>
        /// <param name="targetType">The type of the expected output value</param>
        /// <returns>Returns true if the output type is supported for use</returns>
        public bool CanProcessOutputType(Type targetType) => targetType.IsEnum;

        /// <summary>
        /// Try to parse the input string value as a specified target type
        /// </summary>
        /// <param name="inputValue">The input string that is to be processed into the recorded value</param>
        /// <param name="targetType">The target type that is to be output</param>
        /// <param name="value">Passes out the parsed value if available, null if not</param>
        /// <returns>Returns true if the parsed value is valid for use</returns>
        public bool TryProcessToType(in string inputValue, Type targetType, [MaybeNull] out object? value)
        {
            // Try to do a basic string parse process
            try
            {
                value = Enum.Parse(targetType, inputValue, false);
                return true;
            }

            // If anything goes wrong, that's a failure
            catch (Exception ex)
            {
                Logger.Exception($"[{nameof(EnumTypeParserProcessor)}] Failed to parse the input string '{inputValue}' to type '{targetType}'", ex);
                value = default;
                return false;
            }
        }
    }
}
