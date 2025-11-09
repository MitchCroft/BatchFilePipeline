using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Define a graph that will be run once prior to the main processing graph
    /// </summary>
    internal sealed class PreProcessSupportGraph : SupportGraph
    {
        /*----------Variables----------*/
        //PRIVATE

        //TODO: Add variables that will be used by the pre-process support graph for processing

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the graph with the required label
        /// </summary>
        public PreProcessSupportGraph() :
            base("Pre-Process")
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
