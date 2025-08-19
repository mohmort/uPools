using System;
using System.Collections.Generic;

namespace uPools
{
    public interface IObjectPool<T> : IDisposable
    {
        T Rent();
        void Return(T obj);
        
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