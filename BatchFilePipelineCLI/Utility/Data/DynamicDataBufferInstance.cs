using System.Collections;

namespace BatchFilePipelineCLI.Utility.Data
{
    /// <summary>
    /// A handle to an instance of the dynamic data buffer that can be reused when completed
    /// </summary>
    /// <typeparam name="T">The type of data that is to be contained within the stack</typeparam>
    public sealed class DynamicDataBufferInstance<T> : IList<T>, IDisposable
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The buffer instance that is referenced by this instance
        /// </summary>
        private readonly List<T> _buffer;

        /// <summary>
        /// Store the pool that the buffer will need to be returned to when complete
        /// </summary>
        private readonly DynamicDataBuffer<T> _pool;

        /// <summary>
        /// Flags if this instance has been disposed of
        /// </summary>
        private bool _isDisposed;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Gets or sets the element at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set</param>
        /// <returns>The element at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index is not a valid index in the internal list</exception>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public T this[int index]
        {
            get
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
                }
                return _buffer[index];
            }
            set
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
                }
                _buffer[index] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the internal list
        /// </summary>
        /// <returns>The number of elements contained in the list</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public int Count
        {
            get
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
                }
                return _buffer.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the internal list is read-only
        /// </summary>
        /// <returns>True if the internal list is read-only; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public bool IsReadOnly
        {
            get
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
                }
                return ((ICollection<T>)_buffer).IsReadOnly;
            }
        }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the instance with the required elements needed for operation
        /// </summary>
        /// <param name="buffer">The buffer element that will be available to use for the lifetime of this object</param>
        /// <param name="pool">The pool object instance that the buffer will be returned to</param>
        public DynamicDataBufferInstance(List<T> buffer, DynamicDataBuffer<T> pool)
        {
            _buffer = buffer;
            _pool = pool;
            _isDisposed = false;
        }

        /// <summary>
        /// Adds an item to the internal list
        /// </summary>
        /// <param name="item">The object to add to the internal list</param>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public void Add(T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            _buffer.Add(item);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the internal list
        /// </summary>
        /// <param name="collection">The collection of elements that should be added to the end of the internal list</param>
        /// <exception cref="ArgumentNullException">Collection is null</exception>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            _buffer.AddRange(collection);
        }

        /// <summary>
        /// Removes all items from the internal list
        /// </summary>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public void Clear()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            _buffer.Clear();
        }

        /// <summary>
        /// Determines whether the internal list contains a specific value
        /// </summary>
        /// <param name="item">The object to locate in the internal list</param>
        /// <returns>true if item is found in the internal list; otherwise, false</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public bool Contains(T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            return _buffer.Contains(item);
        }

        /// <summary>
        /// Copies the elemenst of the internal list to an Array starting at a particular Array index
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from the internal list</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins</param>
        /// <exception cref="ArgumentNullException">array is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than 0</exception>
        /// <exception cref="ArgumentException">The number of elements in the source internal list is greater then the available space from arrayIndex to the end of the destination array</exception>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            _buffer.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the internal list
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public IEnumerator<T> GetEnumerator()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            return _buffer.GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific item in the internal list
        /// </summary>
        /// <param name="item">The object to locate in the internal list</param>
        /// <returns>The index of the item if found in the list; otherwise, -1</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public int IndexOf(T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            return _buffer.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item into the internal list at the specified index
        /// </summary>
        /// <param name="index">The zero-based index at which the item should be inserted</param>
        /// <param name="item">The object to insert into the internal list</param>
        /// <exception cref="ArgumentOutOfRangeException">Index is not a valid index in the internal list</exception>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public void Insert(int index, T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            _buffer.Insert(index, item);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the internal list
        /// </summary>
        /// <param name="item">The object to remove from the internal list</param>
        /// <returns>True if item was successfully removed from the internal list; otherwise, false. Also returns false if item is not found in the list</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public bool Remove(T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            return _buffer.Remove(item);
        }

        /// <summary>
        /// Removes the item at the specified index from the internal list
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a valid index in the internal list</exception>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        public void RemoveAt(int index)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            _buffer.RemoveAt(index);
        }

        /// <summary>
        /// Clear up the internal list and return it to the pool for use
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed == true)
            {
                return;
            }
            _isDisposed = true;
            _pool.Return(_buffer);
        }

        //INTERFACE

        /// <summary>
        /// Returns an enumerator that iterates through the internal list
        /// </summary>
        /// <returns>An enumerator object that can be used to iterate through the collection</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed of and the list is not valid for use</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DynamicDataBufferInstance<T>));
            }
            return ((IEnumerable)_buffer).GetEnumerator();
        }
    }
}
