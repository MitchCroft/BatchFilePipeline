using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Utility.Preserve;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BatchFilePipelineCLI.TypeParsing.Parsers
{
    /// <summary>
    /// Handle the conversions for any type that implements IConvertible
    /// </summary>
    [Preserve]
    internal sealed class ConvertableTypeParserProcessor : TypeParserProcessor<IConvertible>
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Flags the priority of this processor when multiple processors are available for a type
        /// </summary>
        /// <remarks>
        /// The higher the value the greater the priority. Processors with higher priority will be chosen over those with lower priority.
        /// </remarks>
        public override int Priority { get; } = int.MinValue;

        /// <summary>
        /// Flags if children of the specified type can be processed by this parser as well
        /// </summary>
        public override bool CanProcessChildren { get; } = true;

        /*----------Functions----------*/
        //PROTECTED

        /// <summary>
        /// Try to parse the input string value as a specified target type
        /// </summary>
        /// <param name="inputValue">The input string that is to be processed into the recorded value</param>
        /// <param name="targetType">The target type that is to be output</param>
        /// <param name="value">Passes out the parsed value if available, null if not</param>
        /// <returns>Returns true if the parsed value is valid for use</returns>
        protected override bool TryParseToValue(in string inputValue, Type targetType, [MaybeNull] out IConvertible? value)
        {
            // We can try and just do a straight up conversion to the required type
            try
            {
                value = (IConvertible)Convert.ChangeType(inputValue, targetType, CultureInfo.InvariantCulture);
                return true;
            }

            // If anything goes wrong, that's a failure
            catch (Exception ex)
            {
                Logger.Exception($"[{nameof(ConvertableTypeParserProcessor)}] Failed to parse the input string '{inputValue}' to type '{targetType}'", ex);
                value = default;
                return false;
            }
        }
    }
}
