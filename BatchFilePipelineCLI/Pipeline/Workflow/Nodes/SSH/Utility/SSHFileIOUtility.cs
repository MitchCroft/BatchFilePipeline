using BatchFilePipelineCLI.Logging;
using Renci.SshNet;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.SSH.Utility
{
    /// <summary>
    /// Common functionality that is needed to operate File IO operations in SSH operations
    /// </summary>
    public static class SSHFileIOUtility
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Ensure that a directory exists on the remote SSH server, creating it if it does not
        /// </summary>
        /// <param name="sftp">The remote client that will be used to poll and create directories</param>
        /// <param name="path">The path of the file that needs to be processed</param>
        /// <param name="cancellationToken">Cancellation token that controls the lifespan of the operation</param>
        public static async ValueTask CreateRemoteDirectoryAsync(this SftpClient sftp,
                                                                 string path,
                                                                 CancellationToken cancellationToken)
        {
            // Split the path into its segments
            string[] segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Iterate through each segment, building up the path as we go
            string current = string.Empty;
            foreach (var part in segments)
            {
                // Check if this section exists
                current += '/' + part;
                bool stageExists = await sftp.ExistsAsync(current, cancellationToken);
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return;
                }
                if (stageExists == true)
                {
                    continue;
                }

                // We need to create the directory stage
                Logger.Log($"[{nameof(SSHFileIOUtility)}] Creating remote directory '{current}'");
                await sftp.CreateDirectoryAsync(current, cancellationToken);
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return;
                }
                Logger.Log($"[{nameof(SSHFileIOUtility)}] Created remote directory '{current}'");
            }
        }

        /// <summary>
        /// Asynchronously copies a file from a remote SFTP server to a local destination
        /// </summary>
        /// <param name="sftp">The SftpClient instance used to connect to the remote server</param>
        /// <param name="source">The path of the source file on the remote server</param>
        /// <param name="destination">The destination path where the file will be copied</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
        public static async ValueTask CopyRemoteFileAsync(this SftpClient sftp,
                                                          string source,
                                                          string destination,
                                                          CancellationToken cancellationToken,
                                                          bool allowOverwrite = false)
        {
            // Read all of the source information to destination
            using var reader = sftp.OpenRead(source.Replace('\\', '/'));
            using var writer = sftp.Open(destination.Replace('\\', '/'), allowOverwrite ? FileMode.Create : FileMode.CreateNew);

            // Copy the data to the destination
            int data;
            while ((data = reader.ReadByte()) != -1)
            {
                writer.WriteByte((byte)data);
            }

            // Flush the data to the location
            await writer.FlushAsync();
        }
    }
}
