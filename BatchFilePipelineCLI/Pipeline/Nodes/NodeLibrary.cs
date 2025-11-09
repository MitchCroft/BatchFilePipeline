using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using BatchFilePipelineCLI.Logging;

namespace BatchFilePipelineCLI.Pipeline.Nodes
{
    /// <summary>
    /// Handle the creation of a workflow based on the supplied description
    /// </summary>
    internal sealed class NodeLibrary
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The type of the shared interface for all pipeline nodes that are to be processed
        /// </summary>
        private readonly Type _targetNodeType = typeof(IPipelineNode);

        /// <summary>
        /// Cache the <see cref="IPipelineNode"/> types that can be used based on the <see cref="PipelineNodeAttribute.ID"/>
        /// </summary>
        private readonly Dictionary<string, Type> _nodeLookup = new();

        /// <summary>
        /// Store the attribute that is associated with each Node to know how they should be processed
        /// </summary>
        private readonly Dictionary<string, PipelineNodeAttribute> _nodeCharacteristics = new();

        /// <summary>
        /// Store the instances of the shared pipeline nodes that can be used for processing
        /// </summary>
        private readonly Dictionary<string, IPipelineNode> _sharedNodes = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Try to initialise the Node Library based on the any <see cref="IPipelineNode"/> types in the loaded assemblies
        /// </summary>
        /// <returns>Return try if the library could be loaded properly from the available types</returns>
        public bool TryLoadFromAppDomain()
        {
            return TryLoadFromTypes(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsAbstract == false && _targetNodeType.IsAssignableFrom(x) == true));
        }

        /// <summary>
        /// Try to initialise the Node Library based on the supplied <see cref="IPipelineNode"/> types
        /// </summary>
        /// <param name="nodeTypes">A collection of the pipeline node types that can be processed</param>
        /// <returns>Return try if the library could be loaded properly from the available types</returns>
        public bool TryLoadFromTypes(IEnumerable<Type> nodeTypes)
        {
            // Clear any existing data
            _nodeLookup.Clear();
            _nodeCharacteristics.Clear();
            _sharedNodes.Clear();

            // Iterate over the types that are to be tested
            bool success = true;
            foreach (var type in nodeTypes)
            {
                // Check that the type is a valid target type
                if (_targetNodeType.IsAssignableFrom(type) == false)
                {
                    Logger.Error($"Unable to process the type '{type}' as a Pipeline Node. Doesn't implement {nameof(IPipelineNode)}");
                    success = false;
                    continue;
                }

                // Get the characteristics attribute
                var characteristic = type.GetCustomAttribute<PipelineNodeAttribute>(false);
                if (characteristic is null)
                {
                    Logger.Error($"Unable to process the type '{type}' as a Pipeline Node. Missing the {nameof(PipelineNodeAttribute)} attached to describe usage");
                    success = false;
                    continue;
                }

                // Check that the type is valid for use
                if (type.IsAbstract == true)
                {
                    Logger.Error($"Unable to process the type '{type}' as a Pipeline Node. Type is abstract");
                    success = false;
                    continue;
                }
                // TODO: #Mitch - Replace the direct node lookup with factory handlers that can be used to create the nodes, removing the need for the default constructor
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Logger.Error($"Unable to process the type '{type}' as a Pipeline Node. Type is missing a default constructor");
                    success = false;
                    continue;
                }

                // Check that we don't clash with an existing use of the ID
                if (_nodeLookup.TryGetValue(characteristic.ID, out var existingType) == true)
                {
                    Logger.Error($"Unable to process the type '{type}' as a Pipeline Node. The assigned ID '{characteristic.ID}' is already in use by '{existingType}'");
                    success = false;
                    continue;
                }

                // Store the values for later use
                _nodeLookup[characteristic.ID] = type;
                _nodeCharacteristics[characteristic.ID] = characteristic;
            }
            return success;
        }

        /// <summary>
        /// Try to get an instance of the Node with the specified type
        /// </summary>
        /// <param name="id">The unique ID of the Node that is to be retrieved</param>
        /// <param name="node">Passes out the instance of the node to be used if a matching one of the node could be found</param>
        /// <returns>Returns true if an instance of the Node of the specified type could be found, false if no Node with that ID is contained</returns>
        public bool TryGetInstanceOfNode(string id, [NotNullWhen(true)] out IPipelineNode? node)
        {
            // Check if we have a node for the type
            if (_nodeLookup.TryGetValue(id, out var nodeType) == false)
            {
                node = null;
                return false;
            }

            // If this is a shared element, then we can check the cache
            bool isShared = _nodeCharacteristics[id].IsShared;
            if (isShared == true &&
                _sharedNodes.TryGetValue(id, out node) == true)
            {
                return true;
            }

            // We need to create a new instance of the node for use
            node = (IPipelineNode)Activator.CreateInstance(nodeType)!;

            // If this is shared, we can store it for later use
            if (isShared == true)
            {
                _sharedNodes[id] = node;
            }
            return true;
        }

        /// <summary>
        /// Retrieve a list of all of the Node types that were found within the library
        /// </summary>
        /// <returns>Returns an enumerable for all of the nodes and their characteristics</returns>
        public IEnumerable<(Type nodeType, PipelineNodeAttribute characteristics)> GetNodeTypes()
        {
            return _nodeLookup.Select(x => (x.Value, _nodeCharacteristics[x.Key]));
        }
    }
}
