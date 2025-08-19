#if UPOOLS_ADDRESSABLES_SUPPORT && UPOOLS_UNITASK_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace uPools
{
    public sealed class AsyncAddressableGameObjectPool : IAsyncObjectPool<GameObject>
    {
        public AsyncAddressableGameObjectPool(object key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            this.key = key;
        }
        
        public AsyncAddressableGameObjectPool(AssetReferenceGameObject reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            this.key = reference.RuntimeKey;
        }

        readonly object key;
        readonly Stack<GameObject> stack = new(32);
        readonly HashSet<GameObject> rentedItems = new();
        readonly HashSet<GameObject> allManagedObjects = new();
        bool isDisposed;

        public int Count => stack.Count;
        public bool IsDisposed => isDisposed;

        public async UniTask<GameObject> RentAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = await Addressables.InstantiateAsync(key).ToUniTask(cancellationToken: cancellationToken);
                allManagedObjects.Add(obj);
            }
            else
            {
                obj.SetActive(true);
            }

            rentedItems.Add(obj);
            PoolCallbackHelper.InvokeOnRent(obj);
            return obj;
        }

        public async UniTask<GameObject> RentAsync(Transform parent, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = await Addressables.InstantiateAsync(key, parent).ToUniTask(cancellationToken: cancellationToken);
                allManagedObjects.Add(obj);
            }
            else
            {
                obj.transform.SetParent(parent);
                obj.SetActive(true);
            }

            rentedItems.Add(obj);
            PoolCallbackHelper.InvokeOnRent(obj);
            return obj;
        }

        public async UniTask<GameObject> RentAsync(Vector3 position, Quaternion rotation, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = await Addressables.InstantiateAsync(key, position, rotation).ToUniTask(cancellationToken: cancellationToken);
                allManagedObjects.Add(obj);
            }
            else
            {
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }

            rentedItems.Add(obj);
            PoolCallbackHelper.InvokeOnRent(obj);
            return obj;
        }

        public async UniTask<GameObject> RentAsync(Vector3 position, Quaternion rotation, Transform parent)
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = await Addressables.InstantiateAsync(key, position, rotation, parent);
                allManagedObjects.Add(obj);
            }
            else
            {
                obj.transform.SetParent(parent);
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }

            rentedItems.Add(obj);
            PoolCallbackHelper.InvokeOnRent(obj);
            return obj;
        }

        public void Return(GameObject obj)
        {
            ThrowIfDisposed();

            rentedItems.Remove(obj);
            stack.Push(obj);
            obj.SetActive(false);

            PoolCallbackHelper.InvokeOnReturn(obj);
        }

        public void Clear()
        {
            ThrowIfDisposed();

            while (stack.TryPop(out var obj))
            {
                Addressables.ReleaseInstance(obj);
            }
            
            // Clean up any remaining rented items
            foreach (var rentedObj in rentedItems.ToArray())
            {
                Addressables.ReleaseInstance(rentedObj);
            }
            
            rentedItems.Clear();
            allManagedObjects.Clear();
        }

        public async UniTask PrewarmAsync(int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            for (int i = 0; i < count; i++)
            {
                var obj = await Addressables.InstantiateAsync(key).ToUniTask(cancellationToken: cancellationToken);
                allManagedObjects.Add(obj);

                stack.Push(obj);
                obj.SetActive(false);

                PoolCallbackHelper.InvokeOnReturn(obj);
            }
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            Clear();
            isDisposed = true;
        }

        void ThrowIfDisposed()
        {
            if (isDisposed) throw new ObjectDisposedException(GetType().Name);
        }
        
        public IReadOnlyCollection<GameObject> GetAllObjects() => allManagedObjects;
        public IReadOnlyCollection<GameObject> GetRentedObjects() => rentedItems;
        public IReadOnlyCollection<GameObject> GetAvailableObjects() => stack;
    }
}
#endif