using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Graphs
{
    /// <summary>
    /// Defines a graph that will be run at the start and end of the main batch process
    /// </summary>
    internal abstract class SupportGraph : ProcessGraph
    {
        /*----------Variables----------*/
        //PRIVATE

        //TODO: Add variables that will be shared by all support graphs for processing

        /*----------Functions----------*/
        //PROTECTED

        /// <summary>
        /// Create the graph with the required values
        /// </summary>
        /// <param name="graphName">The name that will be applied to the graph for display</param>
        protected SupportGraph(string graphName) :
            base(graphName)
        {}
    }
}
