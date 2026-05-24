using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Cancellation;
using BatchFilePipelineCLI.Utility.Extensions;
using System.Collections;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control
{
    /// <summary>
    /// Allow for a user to select a value for branching path logic
    /// </summary>
    [PipelineNode(nameof(UserBranchNode), ~NodeUsage.Process)]
    internal sealed class UserBranchNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The different properties that will be usable to the Node
        /// </summary>
        private readonly Property _promptProperty = Property.Create
        (
            "Prompt",
            "The message that will be displayed to the user to know what they are selecting",
            "Select the option to use -> "
        );
        private readonly Property _optionsTextProperty = Property.Create
        (
            "OptionsText",
            "A collection of elements separated by the '|' character that will be displayed as the different options. Is not required if the options collection is supplied",
            string.Empty,
            "Option 1|Option 2|Option 3|Option 4"
        );
        private readonly Property _optionsCollectionProperty = Property.Create
        (
            "OptionsCollection",
            "A collection of objects that will be displayed as the different options. Is not required if the options text is supplied",
            (IEnumerable)Array.Empty<object>()
        );
        private readonly Property _defaultSelectionProperty = Property.Create
        (
            "DefaultSelection",
            "The default index that will be selected if the timeout period is reached",
            0
        );
        private readonly Property _timeoutPeriodProperty = Property.Create
        (
            "TimeoutPeriod",
            "The length of time (in seconds) that the branch point will be made available, after which the default will be selected. This prevents pipeline being blocked",
            30
        );
        private readonly Property _allowIndexMatchingProperty = Property.Create
        (
            "AllowIndexMatching",
            "Flags if the user can enter an index to select an option",
            true
        );
        private readonly Property _allowLabelMatchingProperty = Property.Create
        (
            "AllowLabelMatching",
            "Flags if the user can enter the label to select an option",
            true
        );

        /// <summary>
        /// Passes out the different pieces of information that were selected
        /// </summary>
        private readonly Property _selectedIndexProperty = Property.Create
        (
            "SelectedIndex",
            "The index of the option that was selected in the process",
            typeof(int)
        );
        private readonly Property _selectedValueProperty = Property.Create
        (
            "SelectedValue",
            "The value of the object that was selected from the options",
            typeof(object)
        );
        private readonly Property _didTimeoutProperty = Property.Create
        (
            "DidTimeout",
            "A flag that indicates if the selection timed out rather then being selected by the user",
            typeof(bool)
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => [_promptProperty, _optionsTextProperty, _optionsCollectionProperty, _defaultSelectionProperty, _timeoutPeriodProperty, _allowIndexMatchingProperty, _allowLabelMatchingProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_selectedIndexProperty, _selectedValueProperty, _didTimeoutProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                       CancellationToken cancellationToken)
        {
            // Get the pieces of information that are required for processing
            string prompt = context.GetInput<string>(_promptProperty);
            string optionsText = context.GetInput<string>(_optionsTextProperty);
            IEnumerable optionsCollection = context.GetInput<IEnumerable>(_optionsCollectionProperty);
            int defaultSelection = context.GetInput<int>(_defaultSelectionProperty);
            int timeoutPeriod = context.GetInput<int>(_timeoutPeriodProperty);
            bool allowIndexMatching = context.GetInput<bool>(_allowIndexMatchingProperty);
            bool allowLabelMatching = context.GetInput<bool>(_allowLabelMatchingProperty);

            // Determine the collection of elements that will be displayed for use
            List<object> displayOptions = new();
            if (string.IsNullOrEmpty(optionsText) == false)
            {
                displayOptions.AddRange(optionsText.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => string.IsNullOrWhiteSpace(x) == false));
            }
            foreach (var option in optionsCollection)
            {
                displayOptions.Add(option);
            }

            // If there is nothing, then that's a problem
            if (displayOptions.Count == 0)
            {
                throw new ArgumentException($"[{nameof(UserBranchNode)}] Unable to determine any options for user selection");
            }

            // We have our options now, we can show them for selection
            Logger.Log($"[{nameof(UserBranchNode)}] Displaying different options for user selection: {string.Join(", ", displayOptions.Select(x => x != null ? x.ToString() : "Null"))}");

            // If there is no matching enabled, then there is nothing we can do
            int selectedIndex = defaultSelection;
            bool didTimeout = false;
            if (allowIndexMatching == false && allowLabelMatching == false)
            {
                Logger.Error($"[{nameof(UserBranchNode)}] No matching options have been enabled, unable to select anything");
            }

            // Otherwise, we can try to let the user pick something
            else
            {
                // Try to get an index from the user
                using var inputCancellationToken = CancellationStack.PushSource(cancellationToken);

                // We need to get the index of the selected element
                var selectionTask = GetSelectionIndexAsync(prompt, displayOptions, allowIndexMatching, allowLabelMatching, inputCancellationToken);

                // We can try to retrieve the input that will be used
                var finished = await Task.WhenAny
                (
                    selectionTask,
                    Task.Delay(TimeSpan.FromSeconds(timeoutPeriod), cancellationToken: inputCancellationToken).SurpressCancellation()
                );

                // If we finished before the timeout, we can use that value
                if (finished == selectionTask)
                {
                    selectedIndex = selectionTask.Result;
                }
                else
                {
                    didTimeout = true;
                }
            }

            // Check that the selection is valid
            if (selectedIndex < 0 && selectedIndex >= displayOptions.Count)
            {
                throw new InvalidDataException($"[{nameof(UserBranchNode)}] Selection value {selectedIndex} is outside the bounds available (0 - {displayOptions.Count - 1})");
            }

            // We have the values that can be used
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _selectedIndexProperty.Name, selectedIndex },
                    { _selectedValueProperty.Name, displayOptions[selectedIndex] },
                    { _didTimeoutProperty.Name, didTimeout }
                },
                nextNode: displayOptions[selectedIndex].ToString()
            );
        }

        //PRIVATE

        /// <summary>
        /// Prompt the user for a valid selection from the collection of options available
        /// </summary>
        /// <param name="prompt">The prompt text that should be displayed to the user</param>
        /// <param name="displayOptions">The collection of options that should be displayed for selection</param>
        /// <param name="allowIndexMatching">Flags if the user input can match against the index</param>
        /// <param name="allowLabelMatching">Flags if the user input can match against the labels</param>
        /// <param name="cancellationToken">Cancellation token that controls the lifespan of the operation</param>
        /// <returns>Returns the selected index, or -1 if unable</returns>
        private static async Task<int> GetSelectionIndexAsync(string prompt,
                                                              List<object> displayOptions,
                                                              bool allowIndexMatching,
                                                              bool allowLabelMatching,
                                                              CancellationToken cancellationToken)
        {
            // Create a list of the different display values
            string[] displayLabels = displayOptions.Select(x => x != null ? x.ToString()?.Trim() ?? "Null" : "Null").ToArray()!;

            // Try to receive the input from the user
            while (cancellationToken.IsCancellationRequested == false)
            {
                // Display the collection of options that can be selected
                Console.WriteLine($"Available Options ({displayOptions.Count}):\n\t{string.Join("\n\t", displayLabels.Select((o, i) => $"{i}.\t{o}"))}");

                // We want selection input from the user
                Console.Write($"{prompt} ");
                var input = await ConsoleUtility.ReadLineAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested == true)
                {
                    break;
                }

                // If there is no input, show the options again
                if (string.IsNullOrWhiteSpace(input) == true)
                {
                    continue;
                }

                // Try to parse the result to an index number that can be used for index selection
                input = input.Trim();
                if (allowIndexMatching == true &&
                    int.TryParse(input, out int index) == true &&
                    index >= 0 && index < displayOptions.Count)
                {
                    return index;
                }

                // Try to see if the entered text matches any of the values
                if (allowLabelMatching == true)
                {
                    // Look for a matching index
                    int labelIndex = Array.IndexOf(displayLabels, input);
                    if (labelIndex != -1)
                    {
                        return labelIndex;
                    }
                }

                // Unable to find a match for use
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unexpected input value '{input}', couldn't match to option");
                Console.ForegroundColor = ConsoleColor.White;
            }
            return -1;
        }
    }
}
