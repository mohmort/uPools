#if UPOOLS_UNITASK_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace uPools
{
    public abstract class AsyncObjectPoolBase<T> : IAsyncObjectPool<T>
        where T : class
    {
        protected readonly Stack<T> stack = new(32);
        protected readonly HashSet<T> rentedItems = new();
        protected readonly HashSet<T> allManagedObjects = new();
        bool isDisposed;

        protected abstract UniTask<T> CreateInstanceAsync(CancellationToken cancellationToken);
        protected virtual void OnDestroy(T instance) { }
        protected virtual void OnRent(T instance) { }
        protected virtual void OnReturn(T instance) { }

        public async UniTask<T> RentAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            T obj;
            
            if (stack.TryPop(out obj))
            {
                rentedItems.Add(obj);
                OnRent(obj);
                if (obj is IPoolCallbackReceiver receiver) receiver.OnRent();
                return obj;
            }

            obj = await CreateInstanceAsync(cancellationToken);
            allManagedObjects.Add(obj);
            rentedItems.Add(obj);
            OnRent(obj);
            if (obj is IPoolCallbackReceiver receiver2) receiver2.OnRent();
            return obj;
        }

        public void Return(T obj)
        {
            ThrowIfDisposed();
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            
            rentedItems.Remove(obj);
            OnReturn(obj);
            if (obj is IPoolCallbackReceiver receiver) receiver.OnReturn();
            stack.Push(obj);
        }

        public void Clear()
        {
            ThrowIfDisposed();
            while (stack.TryPop(out var obj))
            {
                OnDestroy(obj);
            }
            
            // Clean up any remaining rented items
            foreach (var rentedObj in rentedItems.ToArray())
            {
                OnDestroy(rentedObj);
            }
            
            rentedItems.Clear();
            allManagedObjects.Clear();
        }

        public async UniTask PrewarmAsync(int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            for (int i = 0; i < count; i++)
            {
                var instance = await CreateInstanceAsync(cancellationToken);
                allManagedObjects.Add(instance);
                OnReturn(instance);
                if (instance is IPoolCallbackReceiver receiver) receiver.OnReturn();
                stack.Push(instance);
            }
        }

        public int Count => stack.Count;
        public bool IsDisposed => isDisposed;

        public virtual void Dispose()
        {
            ThrowIfDisposed();
            Clear();
            isDisposed = true;
        }

        void ThrowIfDisposed()
        {
            if (isDisposed) throw new ObjectDisposedException(GetType().Name);
        }
        public IReadOnlyCollection<T> GetAllObjects() => allManagedObjects;
        public IReadOnlyCollection<T> GetRentedObjects() => rentedItems;
        public IReadOnlyCollection<T> GetAvailableObjects() => stack;
    }
}
#endif