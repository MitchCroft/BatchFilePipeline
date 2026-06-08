using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Nodes;
using BatchFilePipelineCLI.Pipeline.Runners;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;
using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Manifest
{
    /// <summary>
    /// The base definition of a Node that can be used to interact with a manifest file that is written to disk for processing
    /// </summary>
    internal abstract class ManifestNodeBase : INode
    {
        /*----------Variables----------*/
        //PROTECTED

        /// <summary>
        /// This property will be the base location of the manifest file that all nodes will need to interact with
        /// </summary>
        protected readonly Property _manifestPathProperty = Property.Create
        (
            "ManifestPath",
            "The path to the manifest file where information is being stored for processing",
            typeof(string),
            "Directory/SubDirectory/manifest.json"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public abstract IList<Property> GetInputProperties();

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public abstract IList<Property> GetOutputProperties();

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public abstract ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                          CancellationToken cancellationToken);

        //PROTECTED

        /// <summary>
        /// Read the manifest data that is stored on disk for processing
        /// </summary>
        /// <param name="path">The path to the manifest file that is to be read</param>
        /// <returns>Returns the collection of manifest data that can be used for processing</returns>
        protected ManifestData ReadManifestData(string path)
        {
            try
            {
                Logger.Log($"[{nameof(ManifestNodeBase)}] Reading manifest data from '{path}'");
                string json = File.ReadAllText(path);
                Logger.Log($"[{nameof(ManifestNodeBase)}] Read manifest data from '{path}'\n{json}");
                return JsonConvert.DeserializeObject<ManifestData>(json) ??
                    new ManifestData();
            }

            // If the file doesn't exist, create a new one
            catch (DirectoryNotFoundException) { return new ManifestData(); }
            catch (FileNotFoundException) { return new ManifestData(); }
        }

        /// <summary>
        /// Write the current supplied manifest data to the specified path
        /// </summary>
        /// <param name="path">The path to where the manifest data should be output</param>
        /// <param name="data">The data object that is to be written to the disc</param>
        protected void WriteManifestData(string path,
                                         ManifestData data)
        {
            string json = JsonConvert.SerializeObject(data);
            Logger.Log($"[{nameof(ManifestNodeBase)}] Writing manifest data to '{path}'\n{json}");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }
    }
}
