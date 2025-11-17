namespace BatchFilePipelineCLI.DynamicProperties
{
    /// <summary>
    /// A property that can be used to describe the different properties a Node can use
    /// </summary>
    public readonly struct Property
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The name of the property that can be referenced in the workflow pipeline to populate the value
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// A description of what the property is and what it can be used for
        /// </summary>
        public readonly string Tooltip;

        /// <summary>
        /// The type of the value that is expected to be assigned to this property
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Flags if this property is required to be defined in the workflow
        /// </summary>
        public readonly bool Required;

        /// <summary>
        /// A string that can be used to define an example of usage for the property, possible values, etc.
        /// </summary>
        /// <remarks>
        /// This is intended for use in the generated example, help output rather then when processing
        /// </remarks>
        public readonly string? Example;

        //PRIVATE

        /// <summary>
        /// Function that will be used to retrieve the default value for this property
        /// </summary>
        private readonly Func<object?>? _defaultValueGetter;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// [Optional] Default value that will be assigned to the property if none is supplied
        /// </summary>
        public object? DefaultValue => _defaultValueGetter?.Invoke();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the property with the required information for use
        /// </summary>
        /// <param name="name">The name of the input that can be referenced in the workflow pipeline to populate the value</param>
        /// <param name="tooltip">A description of what the property is and what it can be used for</param>
        /// <param name="type">The type of the value that is expected to be assigned to this property</param>
        /// <param name="example">[Optional] A string that can be used to define an example of usage for the property, possible values, etc.</param>
        public static Property Create(string name,
                                      string tooltip,
                                      Type type,
                                      string? example = null)
        {
            return new Property
            (
                name,
                tooltip,
                type,
                true,
                GetNullValue,
                example
            );
        }

        /// <summary>
        /// Create the property with the required information for use
        /// </summary>
        /// <param name="name">The name of the input that can be referenced in the workflow pipeline to populate the value</param>
        /// <param name="tooltip">A description of what the property is and what it can be used for</param>
        /// <param name="defaultValue">The default value that should be returned if one isn't defined</param>
        /// <param name="example">[Optional] A string that can be used to define an example of usage for the property, possible values, etc.</param>
        public static Property Create<T>(string name,
                                         string tooltip,
                                         T defaultValue,
                                         string? example = null)
        {
            return new Property
            (
                name,
                tooltip,
                typeof(T),
                false,
                () => defaultValue,
                example
            );
        }

        /// <summary>
        /// Create the property with the required information for use
        /// </summary>
        /// <param name="name">The name of the input that can be referenced in the workflow pipeline to populate the value</param>
        /// <param name="tooltip">A description of what the property is and what it can be used for</param>
        /// <param name="defaultValue">A callback that can be used to retrieve a dynamic default value for use</param>
        /// <param name="example">[Optional] A string that can be used to define an example of usage for the property, possible values, etc.</param>
        public static Property Create<T>(string name,
                                         string tooltip,
                                         Func<T> defaultValue,
                                         string? example = null)
        {
            return new Property
            (
                name,
                tooltip,
                typeof(T),
                false,
                () => defaultValue(),
                example
            );
        }

        /// <summary>
        /// Create the property with all of the required values for processing
        /// </summary>
        /// <param name="name" > The name of the input that can be referenced in the workflow pipeline to populate the value</param>
        /// <param name="tooltip">A description of what the property is and what it can be used for</param>
        /// <param name="type">The type of the value that is expected to be assigned to this property</param>
        /// <param name="required">Flags if the value is required or if the default value can be used</param>
        /// <param name="defaultValue">A callback that can be used to retrieve a dynamic default value for use</param>
        /// <param name="example">A string that can be used to define an example of usage for the property, possible values, etc.</param>
        public static Property CreateDefined(string name,
                                             string tooltip,
                                             Type type,
                                             bool required = true,
                                             Func<object?>? defaultValue = null,
                                             string? example = null)
        {
            return new Property
            (
                name,
                tooltip,
                type,
                required,
                defaultValue ?? GetNullValue,
                example
            );
        }

        /// <summary>
        /// Get the string representation of the property description
        /// </summary>
        public override string ToString() => $"{Name} ({Type})";

        //PRIVATE

        /// <summary>
        /// Create the property with all of the required values for processing
        /// </summary>
        /// <param name="name" > The name of the input that can be referenced in the workflow pipeline to populate the value</param>
        /// <param name="tooltip">A description of what the property is and what it can be used for</param>
        /// <param name="type">The type of the value that is expected to be assigned to this property</param>
        /// <param name="required">Flags if the value is required or if the default value can be used</param>
        /// <param name="defaultValue">A callback that can be used to retrieve a dynamic default value for use</param>
        /// <param name="example">A string that can be used to define an example of usage for the property, possible values, etc.</param>
        private Property(string name,
                         string tooltip,
                         Type type,
                         bool required,
                         Func<object?> defaultValue,
                         string? example)
        {
            Name = name;
            Tooltip = tooltip;
            Type = type;
            Required = required;
            _defaultValueGetter = defaultValue;
            Example = example;
        }

        /// <summary>
        /// Empty function that will be used if no default value is defined
        /// </summary>
        /// <returns>Always returns null</returns>
        private static object? GetNullValue() => null;
    }
}
