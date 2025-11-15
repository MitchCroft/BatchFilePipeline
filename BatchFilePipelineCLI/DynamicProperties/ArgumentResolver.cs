using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.TypeParsing;
using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.DynamicProperties
{
    /// <summary>
    /// Handle the process of parsing argument values into the required use type
    /// </summary>
    internal static class ArgumentResolver
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The string that is used to represent a null value in the argument processing
        /// </summary>
        public const string Null = nameof(Null);

        //PRIVATE

        /// <summary>
        /// The stack of buffer elements that can be used for processing
        /// </summary>
        private static Stack<List<ArgumentToken>> _tokenBufferPool = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Handle the process of resolving a dynamic argument value for use in the pipeline
        /// </summary>
        /// <param name="descriptor">The description of the value within the process that is to be interpretted</param>
        /// <param name="property">The property object that describes how it should be resolved and to what type for use</param>
        /// <param name="environmentVariables">The collection of environment variables that will be used for processing</param>
        /// <param name="runtimeVariables">The collection of runtime variables that will be used for processing</param>
        /// <param name="value">Passes out the value that was determined from the descriptor that can be used</param>
        /// <returns>Returns true if the descriptor could be interpreted properly and the output value is an accurate representation</returns>
        public static bool TryResolveArgument(string? descriptor,
                                              ArgumentDescription property,
                                              Dictionary<string, string?> environmentVariables,
                                              Dictionary<string, object?> runtimeVariables,
                                              [MaybeNull] out object? value)
        {
            // We need a buffer to process the data that is contained
            var buffer = RentBuffer();
            try
            {
                // Attempt to tokenise the descriptor into what we need
                if (TokeniseDescriptor(descriptor, buffer) == false)
                {
                    value = null;
                    return false;
                }

                // Count the stats for the buffer
                int variables = buffer.Sum(x => x.isVariable ? 1 : 0);
                int sections = buffer.Sum(x => x.IsEmpty == false && x.isVariable == false ? 1 : 0);

                // If there are no variables or sections, then we check if there is a default value
                if (variables == 0 && sections == 0)
                {
                    return TryResolveDefaultValue(property, out value);
                }

                // If we only have a variable, we can try to retrieve the value for use
                if (variables == 1 && sections == 0)
                {
                    // Find the variable that is needed
                    string variableName = buffer.FirstOrDefault(x => x.isVariable == true).Value;

                    // Try and resolve the variable that is needed
                    var resolution = TryResolveVariable(variableName, environmentVariables, runtimeVariables, out value);
                    if (resolution == Resolution.Failed)
                    {
                        return false;
                    }

                    // If this was an environment variable, we might need to convert it
                    if (resolution == Resolution.Environment)
                    {
                        return TypeParser.TryParse(value as string ?? string.Empty, property.Type, out value);
                    }

                    // Check that the value is compatible with the required type
                    return IsTypeValidForProperty(value, property);
                }

                // If we only have a single static section, we can try and parse that directly
                if (variables == 0 && sections == 1)
                {
                    return TypeParser.TryParse(buffer.FirstOrDefault(x => x.isVariable == false && x.IsEmpty == false).Value, property.Type, out value);
                }

                // Everything else, we try and stick together and parse as a value that can be used
                int failedResolutions = 0;
                string combinedValue = string.Join(string.Empty, buffer.Select(x =>
                {
                    // If this isn't a variable, juse use the static value
                    if (x.isVariable == false)
                    {
                        return x.Value;
                    }

                    // Try and resolve the variable that is needed
                    var resolution = TryResolveVariable(x.Value, environmentVariables, runtimeVariables, out var resolvedValue);
                    if (resolution == Resolution.Failed)
                    {
                        ++failedResolutions;
                        return $"[>{x.Value}<]";
                    }

                    // Otherwise, we need to try and embed the value as a string
                    return resolvedValue?.ToString() ?? Null;
                }));

                // If anything went wrong, that's a problem
                if (failedResolutions > 0)
                {
                    value = null;
                    return false;
                }

                // Try to parse the result to the required type
                return TypeParser.TryParse(combinedValue, property.Type, out value);
            }

            // Make sure the buffer goes back
            finally
            {
                ReturnBuffer(buffer);
            }
        }

        //PRIVATE

        /// <summary>
        /// Attempt to tokenise the descriptor down into the composite parts that are needed for processing
        /// </summary>
        /// <param name="descriptor">The descriptor that is to be processed</param>
        /// <param name="buffer">The buffer that will contain the different elements that were parsed from the descriptor</param>
        /// <returns>Returns true if the tokenised result is valid for use</returns>
        private static bool TokeniseDescriptor(string? descriptor, List<ArgumentToken> buffer)
        {
            // If the descriptor is empty, then we have nothing to do
            if (string.IsNullOrEmpty(descriptor) == true)
            {
                return true;
            }

            // Attempt to parse the different sections
            int sectionStart = 0;
            bool inVariable = false;
            for (int i = 0; i < descriptor.Length; ++i)
            {
                // If this is inside a variable, we want to look for the end
                if (descriptor[i] == '}' && inVariable == true)
                {
                    buffer.Add(new ArgumentToken
                    {
                        isVariable = true,
                        Value = descriptor.Substring(sectionStart, i - sectionStart)
                    });
                    sectionStart = i + 1;
                    inVariable = false;
                }

                // Otherwise, we want to look for the start
                else if (descriptor[i] == '{' && inVariable == false)
                {
                    // If there were any elements in the previous section, add them
                    if (i - sectionStart > 0)
                    {
                        buffer.Add(new ArgumentToken
                        {
                            isVariable = false,
                            Value = descriptor.Substring(sectionStart, i - sectionStart)
                        });
                    }

                    // Start the new section
                    sectionStart = i + 1;
                    inVariable = true;
                }
            }

            // If we didn't finish the variable scope, that's a problem
            if (inVariable == true)
            {
                Logger.Error($"[{nameof(ArgumentResolver)}] Tokenisation of the descriptor finished before end of variable found:\n{descriptor}");
                return false;
            }
            
            // Capture whatever remained as basic text that can be used
            if (descriptor.Length - sectionStart > 0)
            {
                buffer.Add(new ArgumentToken
                {
                    isVariable = false,
                    Value = descriptor.Substring(sectionStart)
                });
            }
            return true;
        }

        /// <summary>
        /// Try to resolve a variable with the specified name
        /// </summary>
        /// <param name="variable">The name of the variable that is to be resolved</param>
        /// <param name="environmentVariables">The collection of environment variables that will be used for processing</param>
        /// <param name="runtimeVariables">The collection of runtime variables that will be used for processing</param>
        /// <param name="value">Passes out the value that was determined from the descriptor that can be used</param>
        /// <returns>Returns true if the variable could be resolved from the available information</returns>
        private static Resolution TryResolveVariable(string variable,
                                                     Dictionary<string, string?> environmentVariables,
                                                     Dictionary<string, object?> runtimeVariables,
                                                     [MaybeNull] out object? value)
        {
            // First place to look is the runtime variables
            if (runtimeVariables.TryGetValue(variable, out value) == true)
            {
                return Resolution.Runtime;
            }

            // Otherwise, we can look in the environment variables
            if (environmentVariables.TryGetValue(variable, out var envValue) == true)
            {
                value = envValue;
                return Resolution.Environment;
            }

            // Otherwise, we've got nothing
            Logger.Error($"[{nameof(ArgumentResolver)}] Unable to resolve a variable with the name '{variable}'");
            return Resolution.Failed;
        }

        /// <summary>
        /// Try to resolve the default value that is specified for the property
        /// </summary>
        /// <param name="property">The property that is being processed</param>
        /// <param name="value">Passes out the value for the property if available</param>
        /// <returns>Returns true if the value returned is valid for use</returns>
        private static bool TryResolveDefaultValue(ArgumentDescription property,
                                                   [MaybeNull] out object? value)
        {
            // If this is required, then we have a problem
            if (property.Required == true)
            {
                value = null;
                return false;
            }

            // Otherwise, just use the default value
            value = property.DefaultValue;
            return IsTypeValidForProperty(value, property);
        }

        /// <summary>
        /// Check to see if the specified value is valid for use with the property
        /// </summary>
        /// <param name="value">The resolved value that is to be evaluated</param>
        /// <param name="property">The property that the value must work for</param>
        /// <returns>Returns true if the value is valid for use with the property</returns>
        /// <returns>Returns true if the value is valid for use with the property</returns>
        private static bool IsTypeValidForProperty(object? value, ArgumentDescription property)
        {
            // If the value is null, it can only be assigned to compatible types
            if (value == null)
            {
                if (property.Type.IsValueType == false || Nullable.GetUnderlyingType(property.Type) != null)
                {
                    return true;
                }
                LogErrorOutput();
                return false;
            }

            // Check the type against the required rules
            Type sourceType = value.GetType();

            // Check for direct assignment
            if (property.Type.IsAssignableFrom(sourceType) == true)
            {
                return true;
            }

            // Find the valid conversions
            TypeCode targetCode = Type.GetTypeCode(property.Type);
            bool isValid = Type.GetTypeCode(sourceType) switch
            {
                TypeCode.Byte => targetCode is TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.SByte => targetCode is TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.Int16 => targetCode is TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.UInt16 => targetCode is TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.Int32 => targetCode is TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.UInt32 => targetCode is TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.Int64 => targetCode is TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.UInt64 => targetCode is TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.Char => targetCode is TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
                TypeCode.Single => targetCode is TypeCode.Double,
                _ => false
            };

            // If the conversion isn't valid, log it
            if (isValid == false)
            {
                LogErrorOutput();
                return false;
            }
            return true;

            // Local function to log the error message
            void LogErrorOutput()
            {
                Logger.Error($"[{nameof(ArgumentResolver)}] The resolved value '{(value != null ? value : Null)}' {(value != null ? $"({value.GetType()}) " : string.Empty)}for argument '{property}' is not compatible with the required type");
            }
        }

        /// <summary>
        /// Retrieve a buffer from the internal pool that can be used for parsing
        /// </summary>
        /// <returns></returns>
        private static List<ArgumentToken> RentBuffer()
        {
            if (_tokenBufferPool.Count == 0)
            {
                return new List<ArgumentToken>();
            }
            lock (_tokenBufferPool)
            {
                var buffer = _tokenBufferPool.Pop();
                buffer.Clear();
                return buffer;
            }
        }

        /// <summary>
        /// Return the buffer to the internal pool for reuse
        /// </summary>
        /// <param name="buffer">The buffer element that is to be used for processing</param>
        private static void ReturnBuffer(List<ArgumentToken> buffer)
        {
            lock (_tokenBufferPool)
            {
                _tokenBufferPool.Push(buffer);
            }
        }

        /*----------Types----------*/
        //PRIVATE

        /// <summary>
        /// Store the collection of information that is needed to describe a dynamic argument
        /// </summary>
        private struct ArgumentToken
        {
            /*----------Variables----------*/
            //PUBLIC

            /// <summary>
            /// Flags if this entry is a variable or a static value
            /// </summary>
            public bool isVariable;

            /// <summary>
            /// Stores the name of the variable or the static value itself
            /// </summary>
            public string Value;

            /*----------Properties----------*/
            //PUBLIC

            /// <summary>
            /// Helper flag to indicate if the token has any real value that can be processed
            /// </summary>
            public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
        }

        /// <summary>
        /// Define the different methods of resolution that can be used for variables
        /// </summary>
        private enum Resolution
        {
            /// <summary>
            /// Process failed to resolve the variable altogether
            /// </summary>
            Failed = 0,

            /// <summary>
            /// Process was resolved with a runtime value that can be used
            /// </summary>
            Runtime = 1,

            /// <summary>
            /// Process was resovled with an environment variable value that might need to be converted
            /// </summary>
            Environment = 2
        }
    }
}
