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
        /// [Optional] A default value that will be assigned to the property if none is supplied
        /// </summary>
        public readonly object? DefaultValue;

        /// <summary>
        /// A string that can be used to define an example of usage for the property, possible values, etc.
        /// </summary>
        /// <remarks>
        /// This is intended for use in the generated example, help output rather then when processing
        /// </remarks>
        public readonly string? Example;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the property with the required information for use
        /// </summary>
        /// <param name="name">The name of the input that can be referenced in the workflow pipeline to populate the value</param>
        /// <param name="tooltip">A description of what the property is and what it can be used for</param>
        /// <param name="type">The type of the value that is expected to be assigned to this property</param>
        /// <param name="required">Flags if this property is required to be defined in the workflow</param>
        /// <param name="defaultValue">[Optional] A default value that will be assigned to the property if none is supplied</param>
        /// <param name="example">A string that can be used to define an example of usage for the property, possible values, etc.</param>
        public Property(string name,
                        string tooltip,
                        Type type,
                        bool required = true,
                        object? defaultValue = null,
                        string? example = null)
        {
            Name = name;
            Tooltip = tooltip;
            Type = type;
            Required = required;
            if (defaultValue != null &&
                type.IsAssignableFrom(defaultValue.GetType()) == false)
            {
                throw new ArgumentException($"[{nameof(Property)}] The default value '{defaultValue}' assigned to '{name}' isn't assignable from the expected type '{type}'");
            }
            DefaultValue = defaultValue;
            Example = example;
        }

        /// <summary>
        /// Get the string representation of the property description
        /// </summary>
        public override string ToString() => $"{Name} ({Type})";
    }
}
