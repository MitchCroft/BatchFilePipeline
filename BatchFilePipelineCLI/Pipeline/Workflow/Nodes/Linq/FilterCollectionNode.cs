using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Comparison;
using BatchFilePipelineCLI.Utility.Extensions;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Linq
{
    /// <summary>
    /// Provide a Node that can be used to filter a collection of values based on contained properties
    /// </summary>
    [PipelineNode(nameof(FilterCollectionNode), NodeUsage.All)]
    internal sealed class FilterCollectionNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We need a number of different inputs to know how to process the data that is being passed in
        /// </summary>
        private readonly Property _collectionProperty = Property.Create
        (
            "Collection",
            "The collection of values that are to be processed as a part of the comparison",
            typeof(IEnumerable)
        );
        private readonly Property _subValueProperty = Property.Create
        (
            "SubValue",
            "The path to the sub-value of each element in the collection that will be used for the comparison. If empty, will just use the root element",
            string.Empty,
            "Property.Field"
        );
        private readonly Property _filterProperty = Property.Create
        (
            "Filter",
            "The filter value that will be compared against to determine which elements should be passed out of the Node",
            typeof(object)
        );
        private readonly Property _comparisonModeProperty = Property.Create
        (
            "Mode",
            "The method of comparison that will be used when comparing the different values",
            defaultValue: ComparisonMode.Equal
        );
        private readonly Property _resolveProperty = Property.Create
        (
            "Resolve",
            "Flags if the collection should be resolved to the final results. If not, an enumerator will be returned for later use",
            false
        );

        /// <summary>
        /// This node will result in a collection of elements that can be used in later stages
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "Passes out a collection of the different input values that matched the filter",
            typeof(IEnumerable)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_collectionProperty, _subValueProperty, _filterProperty, _comparisonModeProperty, _resolveProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="inputs">The collection of inputs that have been described for this node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public ValueTask<ExecutionResult> ProcessNodeResultAsync(IReadOnlyDictionary<string, object?> inputs,
                                                                 CancellationToken cancellationToken)
        {
            // Try to perform the filter operation on the required elements
            try
            {
                // Retrieve the values that will be used for processing
                IEnumerable collection = (IEnumerable)inputs[_collectionProperty.Name]!;
                string subValuePath = (string)inputs[_subValueProperty.Name]!;
                object filterValue = (object)inputs[_filterProperty.Name]!;
                ComparisonMode mode = (ComparisonMode)inputs[_comparisonModeProperty.Name]!;
                bool resolve = (bool)inputs[_resolveProperty.Name]!;

                // Check if the filter is comparable for processing
                IEnumerable? outputValue = (filterValue is IComparable comparableFilter ?
                    FilterMaybeComparable(collection, mode, subValuePath, comparableFilter) :
                    FilterNotComparable(collection, mode, subValuePath, filterValue)
                );

                // Create the output result that will be used
                return ValueTask.FromResult(new ExecutionResult
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, resolve ? outputValue.ToList() : outputValue }
                    }
                ));
            }

            // If something went wrong, use the exception as the output result
            catch (Exception ex) { return ValueTask.FromResult(new ExecutionResult(ex)); }
        }

        //PRIVATE

        /// <summary>
        /// The elements that are going to be checked against, can't be compared via IComparable
        /// </summary>
        /// <param name="collection">The collection of elements that are to be filtered for use</param>
        /// <param name="mode">The mode that is to be used for processing</param>
        /// <param name="subValuePath">The path that will be used for additional element filtering as required</param>
        /// <param name="filter">The filter object that is going to be compared against</param>
        /// <returns>Returns a collection of the different elements that passed the filter</returns>
        private IEnumerable FilterNotComparable(IEnumerable collection, ComparisonMode mode, string subValuePath, object filter)
        {
            // If the comparison mode isn't equals or not equals, we can't really do anything here
            if (mode is not ComparisonMode.Equal or ComparisonMode.NotEqual)
            {
                Logger.Error($"[{nameof(FilterCollectionNode)}] Unable to process a comparison mode of '{mode}' against the non-IComparable object '{filter}'");
                yield break;
            }

            // Iterate over the elements and see if we can use any of them
            foreach (var item in collection)
            {
                // Find the item to be compared
                object? value = item;
                if (string.IsNullOrWhiteSpace(subValuePath) == false)
                {
                    if (Resolver.TryResolveReflectiveProperty(value, subValuePath, out value) == false)
                    {
                        Logger.Log($"[{nameof(FilterCollectionNode)}] Unable to retrieve the value at path '{subValuePath}' for the item '{item}' for filter comparison");
                        continue;
                    }
                }

                // Perform the check operation
                switch (mode)
                {
                    case ComparisonMode.Equal:
                        if (object.Equals(value, filter) == true)
                        {
                            yield return item;
                        }
                        break;
                    case ComparisonMode.NotEqual:
                        if (object.Equals(value, filter) == false)
                        {
                            yield return item;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// The elements that are going to be checked against, can't be compared via IComparable
        /// </summary>
        /// <param name="collection">The collection of elements that are to be filtered for use</param>
        /// <param name="mode">The mode that is to be used for processing</param>
        /// <param name="subValuePath">The path that will be used for additional element filtering as required</param>
        /// <param name="filter">The filter object that is going to be compared against</param>
        /// <returns>Returns a collection of the different elements that passed the filter</returns>
        private IEnumerable FilterMaybeComparable(IEnumerable collection, ComparisonMode mode, string subValuePath, IComparable filter)
        {
            // Iterate over the elements and see if we can use any of them
            foreach (var item in collection)
            {
                // Find the item to be compared
                object? value = item;
                if (string.IsNullOrWhiteSpace(subValuePath) == false)
                {
                    if (Resolver.TryResolveReflectiveProperty(value, subValuePath, out value) == false)
                    {
                        Logger.Log($"[{nameof(FilterCollectionNode)}] Unable to retrieve the value at path '{subValuePath}' for the item '{item}' for filter comparison");
                        continue;
                    }
                }

                // If this value is comparable, we can use the regular checks
                if (value is IComparable comparableValue)
                {
                    if (ComparisonUtility.Compare(comparableValue, mode, filter, out _) == true)
                    {
                        yield return item;
                    }
                }

                // Otherwise, we have to do a basic check
                else
                {
                    // If the comparison mode isn't equals or not equals, we can't really do anything here
                    if (mode is not ComparisonMode.Equal or ComparisonMode.NotEqual)
                    {
                        Logger.Error($"[{nameof(FilterCollectionNode)}] Unable to process a comparison mode of '{mode}' against the non-IComparable object '{filter}'");
                        yield break;
                    }

                    // Perform the check operation
                    switch (mode)
                    {
                        case ComparisonMode.Equal:
                            if (object.Equals(value, filter) == true)
                            {
                                yield return item;
                            }
                            break;
                        case ComparisonMode.NotEqual:
                            if (object.Equals(value, filter) == false)
                            {
                                yield return item;
                            }
                            break;
                    }
                }
            }
        }

    }
}
