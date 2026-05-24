using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control.Utility;
using BatchFilePipelineCLI.PropertyResolver;
using BatchFilePipelineCLI.Utility.Cancellation;
using BatchFilePipelineCLI.Utility.Extensions;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Control
{
    /// <summary>
    /// Allow for the user to enter a string value at runtime for processing
    /// </summary>
    [PipelineNode(nameof(UserInputNode), ~NodeUsage.Process)]
    internal sealed class UserInputNode : IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The different properties that will be usable by the Node
        /// </summary>
        private readonly Property _promptProperty = Property.Create
        (
            "Prompt",
            "The message that will be displayed to the user to know what they are entering",
            typeof(string),
            "User Input "
        );
        private readonly Property _defaultValueProperty = Property.Create
        (
            "Default",
            "The default value that will be used if the user doesn't enter anything",
            string.Empty
        );
        private readonly Property _timeoutPeriodProperty = Property.Create
        (
            "TimeoutPeriod",
            "The length of time (in seconds) that the branch point will be made available, after which the default will be selected. This prevents pipeline being blocked",
            30
        );

        /// <summary>
        /// Passes out the different pieces of information that were entered
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The text that was entered by the user or default value if nothing was entered",
            typeof(string)
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
        public IList<Property> GetInputProperties() => [_promptProperty, _defaultValueProperty, _timeoutPeriodProperty];

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty, _didTimeoutProperty];

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
            string defaultValue = context.GetInput<string>(_defaultValueProperty);
            int timeoutPeriod = context.GetInput<int>(_timeoutPeriodProperty);

            // We need to try and get the value from the user
            using var inputCancellationToken = CancellationStack.PushSource(cancellationToken);
            var inputTask = GetUserInputAsync(prompt, defaultValue, inputCancellationToken.Token);

            // We can try to retrieve the input before the timeout period is hit
            var finished = await Task.WhenAny
            (
                inputTask,
                Task.Delay(TimeSpan.FromSeconds(timeoutPeriod), cancellationToken: inputCancellationToken).SurpressCancellation()
            );

            // If we finished before the timeout, then we can use that value
            string finalResult;
            bool didTimeout = false;
            if (finished == inputTask)
            {
                finalResult = inputTask.Result;
            }
            else
            {
                finalResult = defaultValue;
                didTimeout = true;
            }

            // We have the values that can be returned
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, finalResult },
                    { _didTimeoutProperty.Name, didTimeout }
                }
            );
        }

        //PRIVATE

        /// <summary>
        /// Prompt the user for a string value to be entered that can be used in the pipeline for processing
        /// </summary>
        /// <param name="prompt">The prompt that is to be displayed to the user</param>
        /// <param name="defaultValue">The default value that will be used if the user doesn't enter anything</param>
        /// <param name="cancellationToken">Cancellation token that controls the lifespan of the operation</param>
        /// <returns>Returns the string the user entered, or the default value if none entered</returns>
        private static async Task<string> GetUserInputAsync(string prompt,
                                                            string defaultValue,
                                                            CancellationToken cancellationToken)
        {
            // List if there is a default value
            if (string.IsNullOrWhiteSpace(defaultValue) == false)
            {
                Console.WriteLine($"Default Value = \"{defaultValue}\"");
            }

            // Show the prompt to the user
            Console.Write(prompt);
            var input = await ConsoleUtility.ReadLineAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested == true)
            {
                return defaultValue;
            }

            // If there is no input, then we can use the default value
            if (string.IsNullOrWhiteSpace(input) == true)
            {
                return defaultValue;
            }

            // We've got the user input, give it up
            return input;
        }
    }
}
