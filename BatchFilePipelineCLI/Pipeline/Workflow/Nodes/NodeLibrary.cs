using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Nodes;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes
{
    /// <summary>
    /// Handle the creation of a workflow based on the supplied description
    /// </summary>
    public sealed class NodeLibrary
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The type of the shared interface for all pipeline nodes that are to be processed
        /// </summary>
        private readonly Type _targetNodeType = typeof(INode);

        /// <summary>
        /// Cache the <see cref="INode"/> types that can be used based on the name of the node
        /// </summary>
        private readonly Dictionary<string/*TypeId*/, Type> _nodeLookup = new();

        /// <summary>
        /// Store the attribute that is associated with each Node to know how they should be processed
        /// </summary>
        private readonly Dictionary<string/*TypeId*/, PipelineNodeAttribute> _nodeCharacteristics = new();

        /// <summary>
        /// Store the instances of the shared pipeline nodes that can be used for processing
        /// </summary>
        private readonly Dictionary<string/*TypeId*/, INode> _sharedNodes = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Try to initialise the Node Library based on the any <see cref="INode"/> types in the loaded assemblies
        /// </summary>
        /// <returns>Return try if the library could be loaded properly from the available types</returns>
        public bool TryLoadFromAppDomain()
        {
            return TryLoadFromTypes(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsAbstract == false && _targetNodeType.IsAssignableFrom(x) == true));
        }

        /// <summary>
        /// Try to initialise the Node Library based on the supplied <see cref="INode"/> types
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
                    Logger.Error($"Unable to process the type '{type}' as a Pipeline Node. Doesn't implement {nameof(INode)}");
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

                // Use the name of the class as the ID for the node
                string nodeTypeId = type.Name;
                if (_nodeLookup.TryGetValue(nodeTypeId, out var existingType) == true)
                {
                    Logger.Error($"Unable to process the type '{type}' as a Pipeline Node. The ID '{nodeTypeId}' is already in use by '{existingType}'");
                    success = false;
                    continue;
                }

                // Store the values for later use
                _nodeLookup[nodeTypeId] = type;
                _nodeCharacteristics[nodeTypeId] = characteristic;
            }
            return success;
        }

        /// <summary>
        /// Try to retrieve the characteristics for the Node with the specified type ID
        /// </summary>
        /// <param name="typeId">The unique ID of the Nodes that is to be retrieved</param>
        /// <param name="characteristics">Passes out the instance of the characteristics when a matching node is found for processing</param>
        /// <returns>Returns true if node characteristics with the specified ID can be found</returns>
        public bool TryGetNodeCharacteristics(string typeId, [NotNullWhen(true)] out PipelineNodeAttribute? characteristics)
        {
            if (TryResolveValidNodeTypeId(typeId, out var validTypeId) == false)
            {
                characteristics = null;
                return false;
            }
            return _nodeCharacteristics.TryGetValue(validTypeId, out characteristics);
        }

        /// <summary>
        /// Try to get an instance of the Node with the specified type
        /// </summary>
        /// <param name="typeId">The unique ID of the Node that is to be retrieved</param>
        /// <param name="node">Passes out the instance of the node to be used if a matching one of the node could be found</param>
        /// <returns>Returns true if an instance of the Node of the specified type could be found, false if no Node with that ID is contained</returns>
        public bool TryGetInstanceOfNode(string typeId, [NotNullWhen(true)] out INode? node)
        {
            // Check if we have a node for the type
            if (TryResolveValidNodeTypeId(typeId, out var validTypeId) == false ||
                _nodeLookup.TryGetValue(validTypeId, out var nodeType) == false)
            {
                node = null;
                return false;
            }

            // If this is a shared element, then we can check the cache
            bool isShared = _nodeCharacteristics[typeId].IsShared;
            if (isShared == true &&
                _sharedNodes.TryGetValue(typeId, out node) == true)
            {
                return true;
            }

            // We need to create a new instance of the node for use
            node = (INode)Activator.CreateInstance(nodeType)!;

            // If this is shared, we can store it for later use
            if (isShared == true)
            {
                _sharedNodes[typeId] = node;
            }
            return true;
        }

        /// <summary>
        /// Retrieve a list of all of the Node types that were found within the library
        /// </summary>
        /// <returns>Returns an enumerable for all of the nodes and their characteristics</returns>
        public IEnumerable<(Type nodeType, PipelineNodeAttribute characteristics)> GetNodeTypes() =>
            _nodeLookup.Select(x => (x.Value, _nodeCharacteristics[x.Key]));

        //PRIVATE

        /// <summary>
        /// Try to find a valid Node Type ID that can be used for the supplied input
        /// </summary>
        /// <param name="inputTypeId">The specified Node Type ID that has been provided</param>
        /// <param name="nodeTypeId">Passes out the valid Node Type ID that can be used for identification</param>
        /// <returns>Returns true if a matching, valid Type ID could be found</returns>
        private bool TryResolveValidNodeTypeId(string inputTypeId, [NotNullWhen(true)] out string? nodeTypeId)
        {
            // Check if there is a match for the default input
            if (_nodeCharacteristics.ContainsKey(inputTypeId) == true)
            {
                nodeTypeId = inputTypeId;
                return true;
            }

            // Check if there is a match for the input with the "Node" suffix added, as this is a common convention
            inputTypeId += "Node";
            if (_nodeCharacteristics.ContainsKey(inputTypeId) == true)
            {
                nodeTypeId = inputTypeId;
                return true;
            }

            // No match could be found for use
            nodeTypeId = null;
            return false;
        }
    }
}
