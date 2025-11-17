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
        /// Handle the process of resolving an environment variable for use in the pipeline
        /// </summary>
        /// <param name="property">The property that is to be retrieved from the environment variables for use</param>
        /// <param name="environmentVariables">The collection of environment variables that can be pulled from</param>
        /// <param name="value">Passes out the value that was determine from the available information for use</param>
        /// <returns>Returns true if the value is valid for use</returns>
        public static bool TryResolveEnvironmentVariable(Property property,
                                                         Dictionary<string, string?> environmentVariables,
                                                         [MaybeNull] out object? value)
        {
            // If there is no environment variable with the name, we can try to use the default value
            if (environmentVariables.TryGetValue(property.Name, out var envValue) == false)
            {
                // If the variable is required, we have a problem
                if (property.Required == true)
                {
                    Logger.Error($"[{nameof(ArgumentResolver)}] Property '{property}' is required but no environment variable was supplied");
                    value = default;
                    return false;
                }

                // We have a default value that should ideally be useful for the type
                return TryResolveRuntimeValue(property.DefaultValue, property, out value);
            }

            // Otherwise, we need to try and parse it from the value that was set
            return TypeParser.TryParse(envValue ?? string.Empty, property.Type, out value);
        }

        /// <summary>
        /// Handle the process of resolving an environment variable for use in the pipeline
        /// </summary>
        /// <typeparam name="T">The expected type of the value to be retrieved from the environment variable</typeparam>
        /// <param name="property">The property that is to be retrieved from the environment variables for use</param>
        /// <param name="environmentVariables">The collection of environment variables that can be pulled from</param>
        /// <param name="value">Passes out the value that was determine from the available information for use</param>
        /// <returns>Returns true if the value is valid for use</returns>
        public static bool TryResolveEnvironmentVariable<T>(Property property,
                                                            Dictionary<string, string?> environmentVariables,
                                                            [MaybeNull] out T? value)
        {
            // If there is no environment variable with the name, we can try to use the default value
            if (environmentVariables.TryGetValue(property.Name, out var envValue) == false)
            {
                // If the variable is required, we have a problem
                if (property.Required == true)
                {
                    Logger.Error($"[{nameof(ArgumentResolver)}] Property '{property}' is required but no environment variable was supplied");
                    value = default;
                    return false;
                }

                // Try to resolve the value from the default
                if (TryResolveRuntimeValue(property.DefaultValue, property, out var runtimeValue) == false)
                {
                    Logger.Error($"[{nameof(ArgumentResolver)}] Failed to resovle the default value for property '{property}' for use");
                    value = default;
                    return false;
                }

                // Try to cast the result to the expected type
                if (runtimeValue is T castValue == false)
                {
                    Logger.Error($"[{nameof(ArgumentResolver)}] The resolved default value for property '{property}' is not compatible with the expected type '{typeof(T)}'");
                    value = default;
                    return false;
                }

                // We're good to go
                value = castValue;
                return true;
            }

            // Otherwise, we need to try and parse it from the value that was set
            return TypeParser.TryParse(envValue ?? string.Empty, out value);
        }

        /// <summary>
        /// Handle the process of resolving a dynamic argument value for use in the pipeline
        /// </summary>
        /// <param name="descriptor">The description of the value within the process that is to be interpretted</param>
        /// <param name="property">The property object that describes how it should be resolved and to what type for use</param>
        /// <param name="environmentVariables">The collection of environment variables that will be used for processing</param>
        /// <param name="runtimeVariables">The collection of runtime variables that will be used for processing</param>
        /// <param name="value">Passes out the value that was determined from the descriptor that can be used</param>
        /// <returns>Returns true if the descriptor could be interpreted properly and the output value is an accurate representation</returns>
        public static bool TryResolveDescription(string? descriptor,
                                                 Property property,
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
                    // If the variable is required, we have a problem
                    if (property.Required == true)
                    {
                        Logger.Error($"[{nameof(ArgumentResolver)}] Property '{property}' is required but no values were supplied");
                        value = null;
                        return false;
                    }

                    // We have a default value that should ideally be useful for the type
                    return TryResolveRuntimeValue(property.DefaultValue, property, out value);
                }

                // If we only have a variable, we can try to retrieve the value for use
                if (variables == 1 && sections == 0)
                {
                    // Find the variable that is needed
                    string variableName = buffer.FirstOrDefault(x => x.isVariable == true).Value;

                    // Try and resolve the variable that is needed
                    var resolution = TryResolveVariable(variableName, environmentVariables, runtimeVariables, out value);
                    switch (resolution)
                    {
                        case Resolution.Runtime:        return TryResolveRuntimeValue(value, property, out value);
                        case Resolution.Environment:    return TypeParser.TryParse(value as string ?? string.Empty, property.Type, out value);
                        default:                        return false;
                    }
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
        /// Verify the runtime value is valid for use with the property
        /// </summary>
        /// <param name="inputValue">The input value that is to be verified for use as the output</param>
        /// <param name="property">The property that is being used for processing</param>
        /// <param name="outputValue">Passes out the value that should be used for processing</param>
        /// <returns>Returns true if the object is valid for use with the property</returns>
        private static bool TryResolveRuntimeValue(object? inputValue,
                                                   Property property,
                                                   [MaybeNull] out object? outputValue)
        {
            // If the value is null, it can only be used with compatible types
            if (inputValue == null)
            {
                outputValue = null;
                if (property.Type.IsValueType == false || Nullable.GetUnderlyingType(property.Type) != null)
                {
                    return true;
                }
                LogErrorOutput();
                return false;
            }

            // Check for direct assignment suitability
            if (property.Type.IsAssignableFrom(inputValue.GetType()) == true)
            {
                outputValue = inputValue;
                return true;
            }

            // If the values are convertible, we can try and convert them
            if (inputValue is IConvertible convertibleValue &&
                typeof(IConvertible).IsAssignableFrom(property.Type) == true)
            {
                try
                {
                    outputValue = Convert.ChangeType(convertibleValue, property.Type);
                    return true;
                }
                catch (Exception ex)
                {
                    LogErrorOutput();
                    Logger.Exception(ex);
                    outputValue = null;
                    return false;
                }
            }

            // Otherwise, there's nothing we can do
            outputValue = null;
            return false;

            // Local function to log the error message
            void LogErrorOutput()
            {
                Logger.Error($"[{nameof(ArgumentResolver)}] The resolved value '{(inputValue != null ? inputValue : Null)}' {(inputValue != null ? $"({inputValue.GetType()}) " : string.Empty)}for argument '{property}' is not compatible with the required type");
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
