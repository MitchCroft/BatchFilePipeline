using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Graphs;
using BatchFilePipelineCLI.PropertyResolver;
using Renci.SshNet;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH
{
    /// <summary>
    /// Define a Node that allows for the identification of files within a directory on a remote SSH server
    /// </summary>
    [PipelineNode(nameof(FindFilesInSSHDirectoryNode), NodeUsage.MainGraph)]
    internal sealed class FindFilesInSSHDirectoryNode : SSHBaseNode, IPipelineNode
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// We will need to get different pieces of information to be able to perform the search operation
        /// </summary>
        private readonly Property _searchDirectoryProperty = Property.Create
        (
            "SearchDirectory",
            "The root directory that should be searched when looking for possible files",
            typeof(string),
            "Parent/Directory/"
        );
        private readonly Property _searchPatternProperty = Property.Create
        (
            "SearchPattern",
            "The pattern that will be used when searching to identify files within the directory that should be returned",
            defaultValue: "*",
            example: "Search patterns like '*.mp4' to retrieve all mp4 files in the directory"
        );
        private readonly Property _searchOptionProperty = Property.Create
        (
            "SearchOption",
            "The method that should be used to search for files in the specified directory",
            defaultValue: SearchOption.AllDirectories
        );

        /// <summary>
        /// This node is going to pass out a collection of files that were identified for use
        /// </summary>
        private readonly Property _outputProperty = Property.Create
        (
            "Output",
            "The resulting collection of file paths to the elements that were identified for use",
            typeof(string[]),
            example: "[ \"Parent/Directory/File1.txt\", \"Parent/Directory/File2.txt\" ]"
        );

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Retrieve the collection of input properties that can be defined for processing the Node
        /// </summary>
        /// <returns>Retrieve the collection of input properties that can be used by the Node for Processing</returns>
        public IList<Property> GetInputProperties() => CombineInputs(_searchDirectoryProperty, _searchPatternProperty, _searchOptionProperty);

        /// <summary>
        /// Retrieve the collection of output properties that will be made available for use in later stages
        /// </summary>
        /// <returns>Returns the collection of output properties that can be used in later stages for processing</returns>
        public IList<Property> GetOutputProperties() => [_outputProperty];

        /// <summary>
        /// Process the pipeline node with the specified inputs and generate a result
        /// </summary>
        /// <param name="context">The context for the currently executing pipline node</param>
        /// <param name="cancellationToken">Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns the output result of the Node describing the operation that was performed</returns>
        public async ValueTask<ExecutionResult> ProcessNodeResultAsync(PipelineExecutionContext context,
                                                                       CancellationToken cancellationToken)
        {
            // Retrieve the properties that will be processed
            ConnectionInfo connectionInfo = GetConnectionInfo(context);
            string searchDirectory = context.GetInput<string>(_searchDirectoryProperty);
            string searchPattern = context.GetInput<string>(_searchPatternProperty);
            SearchOption searchOption = context.GetInput<SearchOption>(_searchOptionProperty);

            // Find out how many filters there are that need processing
            string[] filterSegments = searchPattern.Split('|', StringSplitOptions.RemoveEmptyEntries);

            // If we don't have any filters, then we won't find anything
            if (filterSegments.Length == 0)
            {
                return new ExecutionResult
                (
                    new Dictionary<string, object?>
                    {
                        { _outputProperty.Name, Array.Empty<string>() }
                    }
                );
            }

            // Try to make the SSH connection
            using var sftp = new SftpClient(connectionInfo);
            sftp.Connect();

            // Retrieve the collection of files that are to be processed
            var fileSearch = searchOption == SearchOption.TopDirectoryOnly ?
                EnumerateRootFilesAsync(sftp, searchDirectory, cancellationToken) :
                EnumerateRecursiveFilesAsync(sftp, searchDirectory, cancellationToken);

            // Filter the files that are retrieved based on the filters
            var files = await fileSearch.Where(x => filterSegments.Any(y => FileSystemName.MatchesSimpleExpression(y, x)))
                .OrderBy(x => x)
                .ToArrayAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested == true)
            {
                return new ExecutionResult();
            }

            // We have the results of the file matching
            return new ExecutionResult
            (
                new Dictionary<string, object?>
                {
                    { _outputProperty.Name, files }
                }
            );
        }

        //PRIVATE

        /// <summary>
        /// Retrieve the collection of files that are located in the root of the search directory
        /// </summary>
        /// <param name="sftp">The client that will be used to request the items in the remote location</param>
        /// <param name="searchDirectory">The root directory that is to be searched for possible files</param>
        /// <param name="cancellationToken">Cancellation token that will be used to kill the enumeration</param>
        /// <returns>Returns a collection of the different file paths that could be found on the connection</returns>
        private static async IAsyncEnumerable<string> EnumerateRootFilesAsync(SftpClient sftp,
                                                                              string searchDirectory,
                                                                              [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Logger.Log($"[{nameof(FindFilesInSSHDirectoryNode)}] Looking for files in: {searchDirectory}");
            await foreach (var f in sftp.ListDirectoryAsync(searchDirectory.Replace('\\', '/'), cancellationToken))
            {
                if (f.IsDirectory || f.Name.StartsWith('.'))
                {
                    continue;
                }
                yield return f.FullName;
            }
        }

        /// <summary>
        /// Retrieve the collection of files that are located recursively in the search directory
        /// </summary>
        /// <param name="sftp">The client that will be used to request the items in the remote location</param>
        /// <param name="searchDirectory">The root directory that is to be searched for possible files</param>
        /// <param name="cancellationToken">Cancellation token that will be used to kill the enumeration</param>
        /// <returns>Returns a collection of the different file paths that could be found on the connection</returns>
        private static async IAsyncEnumerable<string> EnumerateRecursiveFilesAsync(SftpClient sftp,
                                                                                   string searchDirectory,
                                                                                   [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Queue<string> unsearched = new Queue<string>();
            unsearched.Enqueue(searchDirectory.Replace('\\', '/'));
            while (unsearched.Count > 0)
            {
                string current = unsearched.Dequeue();
                Logger.Log($"[{nameof(FindFilesInSSHDirectoryNode)}] Looking for files in: {current}");
                await foreach (var f in sftp.ListDirectoryAsync(current, cancellationToken))
                {
                    if (f.Name.StartsWith('.'))
                    {
                        continue;
                    }
                    else if (f.IsDirectory == true)
                    {
                        unsearched.Enqueue(f.FullName);
                    }
                    else
                    {
                        yield return f.FullName;
                    }
                }
            }
        }
    }
}
