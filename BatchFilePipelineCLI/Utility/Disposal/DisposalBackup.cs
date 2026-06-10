using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchFilePipelineCLI.Utility.Disposal
{
    /// <summary>
    /// Provide a central class for <see cref="IDisposable"/> classes to register themselves for cleanup
    /// when the application reaches the end of operation
    /// </summary>
    /// <remarks>
    /// This is a hail-mary approach to ensuring elements are cleaned up. This is when they would be created
    /// in one location and *should* be disposed of in another. They can be registered on creation and this
    /// will hopefully ensure they are disposed of when the program is closing
    /// </remarks>
    public sealed class DisposalBackup : IDisposable
    {
        /*----------Variables----------*/
        //PRIVATE

        /// <summary>
        /// The singleton instance of the backup instance that will be used to clear up stored elements
        /// </summary>
        private static DisposalBackup? _instance;

        /// <summary>
        /// Collection of all callbacks that will need to be cleared up when the objects are cleared up
        /// </summary>
        private event Action? _onDispose;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Clear the elements that are stored in the collection
        /// </summary>
        public void Dispose()
        {
            if (this == _instance)
            {
                _instance = null;
            }
            _onDispose?.Invoke();
            _onDispose = null;
        }

        /// <summary>
        /// Ensure there is a singleton instance that can the registrations that are needed
        /// </summary>
        /// <returns>Returns the instance of the object that can be monitored for processing</returns>
        public static DisposalBackup Init() => _instance ??= new();

        /// <summary>
        /// Add the disposable object to the collection of elements that will be disposed of
        /// </summary>
        public static void Register(IDisposable disposable)
        {
            if (_instance == null)
            {
                return;
            }
            _instance._onDispose -= disposable.Dispose;
            _instance._onDispose += disposable.Dispose;
        }

        /// <summary>
        /// Remove the disposable object from the collection of elements that will be disposed of
        /// </summary>
        public static void Unregister(IDisposable disposable)
        {
            if (_instance == null)
            {
                return;
            }
            _instance._onDispose -= disposable.Dispose;
            disposable.Dispose();
        }
        
        //PRIVATE

        /// <summary>
        /// Make sure that the instance can only be created through the init function
        /// </summary>
        private DisposalBackup() {}
    }
}
