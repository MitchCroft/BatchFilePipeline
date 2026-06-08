namespace BatchFilePipelineCLI.Utility.Data
{
    /// <summary>
    /// Store a collection of buffer elements that can be used for processing
    /// </summary>
    /// <typeparam name="T">The type of data that is to be contained within the stack</typeparam>
    public sealed class DynamicDataBuffer<T>
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// Store dynamic collections of the data that can be used for processing
        /// </summary>
        private readonly Stack<List<T>> _buffers;

        /// <summary>
        /// The starting capacity that will be used for newly created buffers
        /// </summary>
        private readonly int _startingCapacity;

        /// <summary>
        /// Callback function that can be used to cleanup elements within the collection on disposal
        /// </summary>
        private readonly Action<List<T>>? _onCleanup;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Initialise the collection of dynamic data buffers
        /// </summary>
        /// <param name="startingSize">The number of buffers that should be created initially</param>
        /// <param name="capacity">The starting capacity that should be used for the created buffers</param>
        /// <param name="onCleanup">[Optional] Callback function that can be used to cleanup elements within the collection on disposal</param>
        public DynamicDataBuffer(int startingSize, int capacity, Action<List<T>>? onCleanup = null)
        {
            _buffers = new Stack<List<T>>(startingSize);
            _startingCapacity = startingSize;
            _onCleanup = onCleanup;
            for (int i = 0; i < startingSize; ++i)
            {
                _buffers.Push(new List<T>(capacity));
            }
        }

        /// <summary>
        /// Get the next buffer that can be used for processing
        /// </summary>
        /// <returns>Returns a buffer that can be used for processing</returns>
        public List<T> Get()
        {
            if (_buffers.Count == 0)
            {
                return new List<T>(_startingCapacity);
            }
            lock (_buffers)
            {
                var buffer = _buffers.Pop();
                buffer.Clear();
                return buffer;
            }
        }

        /// <summary>
        /// Return the specified buffer to the pool for later re-use
        /// </summary>
        /// <param name="buffer">The buffer that is no longer needed</param>
        /// <exception cref="ArgumentNullException">If the returned buffer is null</exception>
        public void Return(List<T> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            _onCleanup?.Invoke(buffer);
            lock (_buffers)
            {
                _buffers.Push(buffer);
            }
        }

        /// <summary>
        /// Retrieve a disposable wrapped buffer that can be used for processing
        /// </summary>
        /// <returns>Returns an instance object that will manage the use of the buffer element</returns>
        public DynamicDataBufferInstance<T> Rent() => new DynamicDataBufferInstance<T>(Get(), this);
    }
}
