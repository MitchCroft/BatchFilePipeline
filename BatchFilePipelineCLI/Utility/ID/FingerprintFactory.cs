using System.Security.Cryptography;

namespace BatchFilePipelineCLI.Utility.ID
{
    /// <summary>
    /// Handle the generation of <see cref="Fingerprint"/> instances as a way of identifying data being worked with
    /// </summary>
    internal static class FingerprintFactory
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create a fingerprint from a file on disk
        /// </summary>
        /// <param name="filePath">The path of the file that is to be fingerprinted</param>
        /// <param name="chunkSizeMb">The size of the buffer that will be used when taking the print</param>
        /// <param name="cancellationToken">[Optional] Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns a fingerprint object that can be used for evaluating </returns>
        public static async ValueTask<Fingerprint> FingerprintAsync(string filePath, int chunkSizeMb, CancellationToken cancellationToken = default)
        {
            using var fStream = File.OpenRead(filePath);
            return await FingerprintAsync(fStream, chunkSizeMb, cancellationToken);
        }

        /// <summary>
        /// Create a fingerprint from a buffer of memory
        /// </summary>
        /// <param name="buffer">The collection of bytes that are to be processed</param>
        /// <param name="chunkSizeMb">The size of the buffer that will be used when taking the print</param>
        /// <param name="cancellationToken">[Optional] Cancellation token that can be used to control the lifespan of the operation</param>
        /// <returns>Returns a fingerprint object that can be used for evaluating </returns>
        public static async ValueTask<Fingerprint> FingerprintAsync(byte[] buffer, int chunkSizeMb, CancellationToken cancellationToken = default)
        {
            using var mStream = new MemoryStream(buffer);
            return await FingerprintAsync(mStream, chunkSizeMb, cancellationToken);
        }

        /// <summary>
        /// Process the incoming stream of data and determine the fingerprint of the data contained
        /// </summary>
        /// <param name="input">The input stream that is to be processed</param>
        /// <param name="chunkSizeMb">The size, in megabytes, of data to read as a chunk from each end</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the process</param>
        /// <returns>Returns the evaluated fingerprint of the operation</returns>
        public static async ValueTask<Fingerprint> FingerprintAsync(Stream input, int chunkSizeMb, CancellationToken cancellationToken)
        {
            // Determine the size of the buffer to be used
            int chunkSize = chunkSizeMb * 1024 * 1024;
            byte[] buffer = new byte[chunkSize * 2];

            // Read the data from the stream for use
            int readFirst = await input.ReadAsync(buffer, 0, chunkSize, cancellationToken);
            if (readFirst == chunkSize)
            {
                input.Seek(-chunkSize, SeekOrigin.End);
                await input.ReadAsync(buffer, chunkSize, chunkSize, cancellationToken);
            }

            // Create the fingerprint hash
            using var md5 = MD5.Create();
            return new Fingerprint(BitConverter.ToString(md5.ComputeHash(buffer)).Replace("-", string.Empty));
        }
    }
}
