using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.TypeParsing;
using BatchFilePipelineCLI.Utility.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace BatchFilePipelineCLI.PropertyResolver
{
    /// <summary>
    /// Handle the process of parsing the description of variables into a format that can be used for processing
    /// </summary>
    public static class Resolver
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The string value that will be used to represent a null value in the processing pipeline
        /// </summary>
        public const string Null = nameof(Null);

        //PRIVATE

        /// <summary>
        /// Stack of buffers that can be used when resolving required input
        /// </summary>
        private static DynamicDataBuffer<ResolverToken> _tokenPool = new(1, 5, CleanupTokenBuffer);

        /// <summary>
        /// Stack of buffers that can be used when determining the reflection variable segments that need processing
        /// </summary>
        private static DynamicDataBuffer<string> _variablePool = new(3, 5);

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
                                                         IReadOnlyDictionary<string, string?> environmentVariables,
                                                         [MaybeNull] out object? value)
        {
            // If there is no environment variable with the name, we can try to use the default value
            if (environmentVariables.TryGetValue(property.Name, out var envValue) == false)
            {
                // If the variable is required, we have a problem
                if (property.Required == true)
                {
                    Logger.Error($"[{nameof(Resolver)}] Property '{property}' is required but no environment variable was defined");
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
                                                            IReadOnlyDictionary<string, string?> environmentVariables,
                                                            [MaybeNull] out T? value)
        {
            // If there is no environment variable with the name, we can try to use the default value
            if (environmentVariables.TryGetValue(property.Name, out var envValue) == false)
            {
                // If the variable is required, we have a problem
                if (property.Required == true)
                {
                    Logger.Error($"[{nameof(Resolver)}] Property '{property}' is required but no environment variable was defined");
                    value = default;
                    return false;
                }

                // We have a default value that should ideally be useful for the type
                if (TryResolveRuntimeValue(property.DefaultValue, property, out var runtimeValue) == false)
                {
                    Logger.Error($"[{nameof(Resolver)}] Failed to resolve the default value for property '{property}' for use");
                    value = default;
                    return false;
                }

                // Try to cast the result to the expected type
                if (runtimeValue is T castValue == false)
                {
                    Logger.Error($"[{nameof(Resolver)}] The resolved default value for property '{property}' is not compatible with the expected type '{typeof(T)}'");
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
        public static bool TryResolveDescriptor(string? descriptor,
                                                Property property,
                                                IReadOnlyDictionary<string, string?> environmentVariables,
                                                IReadOnlyDictionary<string, object?> runtimeVariables,
                                                [MaybeNull] out object? value)
        {
            // We can rent a buffer for processing the descriptor
            using var buffer = _tokenPool.Rent();

            // Attempt to tokenise the descriptor into what we need
            if (TryTokeniseDescriptor(descriptor, buffer) == false)
            {
                value = null;
                return false;
            }

            // Count the stats for the buffer so we know how to parse this
            int variables = 0;
            int sections = 0;
            for (int i = 0; i < buffer.Count; ++i)
            {
                if (buffer[i].IsVariable == true)
                {
                    ++variables;
                } else if (buffer[i].IsEmpty == false)
                {
                    ++sections;
                }
            }

            // If there are no variables or sections, we can try to use the default
            if (variables == 0 && sections == 0)
            {
                // If the variable is required, then we have a problem
                if (property.Required == true)
                {
                    Logger.Error($"[{nameof(Resolver)}] Property '{property}' is required but no values were supplied");
                    value = null;
                    return false;
                }

                // Try to use the default value that is defined
                return TryResolveRuntimeValue(property.DefaultValue, property, out value);
            }

            // If we only have a variable, we can try to just use that single value
            if (variables == 1 && sections == 0)
            {
                // Find the variable that is needed
                var variablePath = buffer.FirstOrDefault(x => x.IsVariable == true).Values;

                // Try and resolve the variable that is needed
                var resolution = TryResolveVariable(variablePath, environmentVariables, runtimeVariables, out value);
                switch (resolution)
                {
                    case Resolution.Runtime:        return TryResolveRuntimeValue(value, property, out value);
                    case Resolution.Environment:    return TypeParser.TryParse(value as string ?? string.Empty, property.Type, out value);
                    default:                        return false;
                }
            }

            // If we only have a single, static section, we can try and parse that directly
            if (variables == 0 && sections == 1)
            {
                var element = buffer.FirstOrDefault(x => x.IsVariable == false && x.IsEmpty == false);
                string sectionValue = string.Empty;
                if (element.IsEmpty == false)
                {
                    sectionValue = element.Values![0];
                }
                return TypeParser.TryParse
                (
                    sectionValue,
                    property.Type,
                    out value
                );
            }

            // For everything else, we try to stick together all the parts and parse that as a single value
            int failedResolutions = 0;
            string combinedValue = string.Join(string.Empty, buffer.Select(x =>
            {
                // If this is empty, just use an empty value
                if (x.IsEmpty == true)
                {
                    return string.Empty;
                }

                // If this isn't a variable, just use the static value
                if (x.IsVariable == false)
                {
                    return x.Values![0];
                }

                // Try and resolve the variable that is needed
                var resolution = TryResolveVariable(x.Values, environmentVariables, runtimeVariables, out var resolvedValue);
                if (resolution == Resolution.Failed)
                {
                    ++failedResolutions;
                    return $"[>{string.Join('.', x.Values!)}<]";
                }

                // Otherwise, try to convert the value to string for embedding
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

        /// <summary>
        /// Handle the process of resolving a dynamic argument value for use in the pipeline without a type guide
        /// </summary>
        /// <param name="descriptor">The description of the value within the process that is to be interpretted</param>
        /// <param name="environmentVariables">The collection of environment variables that will be used for processing</param>
        /// <param name="runtimeVariables">The collection of runtime variables that will be used for processing</param>
        /// <param name="value">Passes out the value that was determined from the descriptor that can be used</param>
        /// <returns>Returns true if the descriptor could be interpreted properly</returns>
        /// <remarks>
        /// This function doesn't provide any type validation or verification, the output type is not controlled for at all.
        /// 
        /// If the descriptor is just for a single runtime variable, the output type will have the same output.
        /// Any composites or environment variables will return a string
        /// </remarks>
        public static bool TryResolveLooseDescriptor(string? descriptor,
                                                     IReadOnlyDictionary<string, string?> environmentVariables,
                                                     IReadOnlyDictionary<string, object?> runtimeVariables,
                                                     [MaybeNull] out object? value)
        {
            // We can rent a buffer for processing the descriptor
            using var buffer = _tokenPool.Rent();

            // Attempt to tokenise the descriptor into what we need
            if (TryTokeniseDescriptor(descriptor, buffer) == false)
            {
                value = null;
                return false;
            }

            // Count the stats for the buffer so we know how to parse this
            int variables = 0;
            int sections = 0;
            for (int i = 0; i < buffer.Count; ++i)
            {
                if (buffer[i].IsVariable == true)
                {
                    ++variables;
                }
                else if (buffer[i].IsEmpty == false)
                {
                    ++sections;
                }
            }

            // If it's empty, there's nothing we can figure out
            if (variables == 0 && sections == 0)
            {
                value = null;
                return false;
            }

            // If we only have a variable, we can try to just use that single value
            if (variables == 1 && sections == 0)
            {
                // Find the variable that is needed
                var variablePath = buffer.FirstOrDefault(x => x.IsVariable == true).Values;

                // Try and resolve the variable that is needed
                return TryResolveVariable(variablePath, environmentVariables, runtimeVariables, out value) != Resolution.Failed;
            }

            // If we only have a single, static section, we can try and parse that directly
            if (variables == 0 && sections == 1)
            {
                var element = buffer.FirstOrDefault(x => x.IsVariable == false && x.IsEmpty == false);
                value = string.Empty;
                if (element.IsEmpty == false)
                {
                    value = element.Values![0];
                }
                return true;
            }

            // For everything else, we try to stick together all the parts and parse that as a single value
            int failedResolutions = 0;
            string combinedValue = string.Join(string.Empty, buffer.Select(x =>
            {
                // If this is empty, just use an empty value
                if (x.IsEmpty == true)
                {
                    return string.Empty;
                }

                // If this isn't a variable, just use the static value
                if (x.IsVariable == false)
                {
                    return x.Values![0];
                }

                // Try and resolve the variable that is needed
                var resolution = TryResolveVariable(x.Values, environmentVariables, runtimeVariables, out var resolvedValue);
                if (resolution == Resolution.Failed)
                {
                    ++failedResolutions;
                    return $"[>{string.Join('.', x.Values!)}<]";
                }

                // Otherwise, try to convert the value to string for embedding
                return resolvedValue?.ToString() ?? Null;
            }));

            // If anything went wrong, that's a problem
            if (failedResolutions > 0)
            {
                value = null;
                return false;
            }

            // Try to parse the result to the required type
            value = combinedValue;
            return true;
        }

        /// <summary>
        /// Try to process the identification of a value from an object via reflection
        /// </summary>
        /// <param name="source">The source object that is being processed for value identification</param>
        /// <param name="path">The '.' separated path of the fields/properties to be traversed</param>
        /// <param name="value">Passes out the value of the determined property, if it can be found</param>
        /// <returns>Returns true if the value is valid for use</returns>
        public static bool TryResolveReflectiveProperty(object? source,
                                                        string path,
                                                        [MaybeNull] out object? value)
        {
            // If the source object is null, nothing we can do
            if (source == null)
            {
                value = null;
                return false;
            }

            // Find the property path that is to be processed
            string[] segments = path.Split('.');

            // Try to parse the value
            for (int i = 0; i < segments.Length && source != null; ++i)
            {
                if (TryFindReflectiveValue(source, segments[i], out source) == false)
                {
                    value = null;
                    return false;
                }
            }

            // We have a value that can be used
            value = source;
            return true;
        }

        //PRIVATE

        /// <summary>
        /// Attempt to tokenise the descriptor down into the composite parts that are needed for processing
        /// </summary>
        /// <param name="descriptor">The descriptor that is to be processed</param>
        /// <param name="buffer">The buffer that will contain the different elements that were parsed from the descriptor</param>
        /// <returns>Returns true if the tokenised result is valid for use</returns>
        private static bool TryTokeniseDescriptor(string? descriptor, in DynamicDataBufferInstance<ResolverToken> buffer)
        {
            // If the descriptor is empty, then we have nothing to do
            if (string.IsNullOrWhiteSpace(descriptor) == true)
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
                    // Extract the string section that makes up the variable section
                    string section = descriptor.Substring(sectionStart, i - sectionStart);

                    // Setup the variables for use
                    var token = new ResolverToken
                    {
                        IsVariable = true,
                        Values = _variablePool.Rent()
                    };

                    // Add the different segments in the variable section
                    token.Values.AddRange(section.Split('.'));
                    buffer.Add(token);

                    // Progress values
                    sectionStart = i + 1;
                    inVariable = false;
                }

                // Look for the start of a variable
                else if (descriptor[i] == '{' && inVariable == false)
                {
                    // If there were any elements in the previous section, add them
                    if (i - sectionStart > 0)
                    {
                        // Setup the token for use
                        var token = new ResolverToken
                        {
                            IsVariable = false,
                            Values = _variablePool.Rent()
                        };
                        token.Values.Add(descriptor.Substring(sectionStart, i - sectionStart));
                        buffer.Add(token);
                    }

                    // Start the new section
                    sectionStart = i + 1;
                    inVariable = true;
                }
            }

            // If we didn't finish the variable scope, that's a problem
            if (inVariable == true)
            {
                Logger.Error($"[{nameof(Resolver)}] Tokenisation of the descriptor finished before end of variable found:\n{descriptor}");
                return false;
            }

            // Capture whatever remained as basic text that can be used
            if (descriptor.Length - sectionStart > 0)
            {
                // Setup the token for use
                var token = new ResolverToken
                {
                    IsVariable = false,
                    Values = _variablePool.Rent()
                };
                token.Values.Add(descriptor.Substring(sectionStart));
                buffer.Add(token);
            }
            return true;
        }

        /// <summary>
        /// Dispose of the containers of variable segments within the tokens
        /// </summary>
        /// <param name="buffer">The list collection that is to be cleaned up</param>
        private static void CleanupTokenBuffer(IReadOnlyList<ResolverToken> buffer)
        {
            for (int i = 0; i < buffer.Count; ++i)
            {
                buffer[i].Values?.Dispose();
            }
        }

        /// <summary>
        /// Try to resolve a variable with the specified path
        /// </summary>
        /// <param name="variablePath">The path of the variable for the data to retrieve</param>
        /// <param name="environmentVariables">The collection of environment variables that will be used for processing</param>
        /// <param name="runtimeVariables">The collection of runtime variables that will be used for processing</param>
        /// <param name="value">Passes out the value that was determined from the descriptor that can be used</param>
        /// <returns>Returns the source of the variable that was resolved from the path or failed if unable</returns>
        private static Resolution TryResolveVariable(DynamicDataBufferInstance<string>? variablePath,
                                                     IReadOnlyDictionary<string, string?> environmentVariables,
                                                     IReadOnlyDictionary<string, object?> runtimeVariables,
                                                     [MaybeNull] out object? value)
        {
            // If there is nothing, then we can't retrieve a value
            value = null;
            if (variablePath == null || variablePath.Count == 0)
            {
                return Resolution.Failed;
            }

            // Look for the value that is needed for processing
            string anchorName = variablePath[0];
            Resolution source = Resolution.Failed;
            if (runtimeVariables.TryGetValue(anchorName, out var stagingValue) == true)
            {
                source = Resolution.Runtime;
            }
            else if (environmentVariables.TryGetValue(anchorName, out var stagingEnvValue) == true)
            {
                source = Resolution.Environment;
                stagingValue = stagingEnvValue;
            }
            else
            {
                return Resolution.Failed;
            }

            // If we've got nothing, then we can't do anything with
            if (source == Resolution.Failed)
            {
                return Resolution.Failed;
            }

            // If there is only one stage to the variable, we have our answer
            value = stagingValue;
            if (variablePath.Count == 1)
            {
                return source;
            }

            // We're now going to be using reflection to try and grab the values and properties defined in the available options
            // All values from this point are are going to be runtime values
            source = Resolution.Runtime;
            for (int i = 1; i < variablePath.Count && value != null; ++i)
            {
                if (TryFindReflectiveValue(value, variablePath[i], out value) == false)
                {
                    break;
                }
            }

            // We have the result
            return source;
        }

        /// <summary>
        /// Try to find the source value behind the specified name
        /// </summary>
        /// <param name="source">The source object that is to be searched</param>
        /// <param name="name">The name of the value field/property to retrieve</param>
        /// <param name="value">Passes out the value that was found in the corresponding location or null if unavailble</param>
        /// <returns>Returns true if the returned value is valid and was found for the object</returns>
        private static bool TryFindReflectiveValue(object source,
                                                   string name,
                                                   [MaybeNull] out object value)
        {
            // If the source object is null, then we can't find anything
            if (source == null)
            {
                value = null;
                return false;
            }

            // Get the type for the object that is being checked
            Type? activeType = source.GetType();

            // We need to find the value that is being used
            const BindingFlags SEARCH_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            do
            {
                // Check for a field that matches the name
                FieldInfo? field = activeType.GetField(name, SEARCH_FLAGS);
                if (field != null)
                {
                    value = field.GetValue(source);
                    return true;
                }

                // Check for a property that matches the name
                PropertyInfo? property = activeType.GetProperty(name, SEARCH_FLAGS);
                if (property != null)
                {
                    value = property.GetValue(source);
                    return true;
                }

                // Try the next level up the hierarchy chain
                activeType = activeType.BaseType;
            }
            while (activeType != null);

            // If we couldn't find anything, nothing we can do
            value = null;
            return false;
        }

        /// <summary>
        /// Verify the runtime value is alid for use with the property
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
                    outputValue = Convert.ChangeType(convertibleValue, property.Type, CultureInfo.InvariantCulture);
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
                Logger.Error($"[{nameof(Resolver)}] The resolved value '{(inputValue != null ? inputValue : Null)}' {(inputValue != null ? $"({inputValue.GetType()}) " : string.Empty)}for argument '{property}' is not compatible with the required type");
            }
        }

        /*----------Types----------*/
        //PUBLIC

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
