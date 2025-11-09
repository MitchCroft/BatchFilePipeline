using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// The main processing graph that will be run for all files collected within the batch catchment
    /// </summary>
    internal sealed class MainProcessGraph : ProcessGraph
    {
        /*----------Variables----------*/
        //PRIVATE

        //TODO: Add variables that will be used by the main process

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the graph with the required label
        /// </summary>
        public MainProcessGraph() :
            base("Main Process")
        {}

        //PROTECTED

        /// <summary>
        /// Check to see that all of the starting information is defined for the operation to progress
        /// </summary>
        /// <param name="environmentVariables">The collection of environment variables that have been defined for use</param>
        /// <returns>Returns true if the required information is present and available for processing</returns>
        protected override bool IdentifyStartingValues(Dictionary<string, string?> environmentVariables)
        {
            // TODO: Check that the values are good to go
            return true;
        }
    }
}
