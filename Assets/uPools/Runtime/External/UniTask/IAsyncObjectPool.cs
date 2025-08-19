#if UPOOLS_UNITASK_SUPPORT
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace uPools
{
    public interface IAsyncObjectPool<T>
    {
        UniTask<T> RentAsync(CancellationToken cancellationToken);
        void Return(T instance);
        
        /// <summary>
        /// Gets all objects managed by this pool (both available and rented)
        /// </summary>
        IReadOnlyCollection<T> GetAllObjects();
        
        /// <summary>
        /// Gets objects that are currently rented out from the pool
        /// </summary>
        IReadOnlyCollection<T> GetRentedObjects();
        
        /// <summary>
        /// Gets objects that are currently available in the pool
        /// </summary>
        IReadOnlyCollection<T> GetAvailableObjects();
    }
}
#endif