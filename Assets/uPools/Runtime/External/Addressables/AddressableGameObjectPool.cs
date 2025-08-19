#if UPOOLS_ADDRESSABLES_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace uPools
{
    public sealed class AddressableGameObjectPool : IObjectPool<GameObject>
    {
        public AddressableGameObjectPool(object key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            this.key = key;
        }

        public AddressableGameObjectPool(AssetReferenceGameObject reference)
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

        public GameObject Rent()
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = Addressables.InstantiateAsync(key).WaitForCompletion();
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

        public GameObject Rent(Transform parent)
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = Addressables.InstantiateAsync(key, parent).WaitForCompletion();
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

        public GameObject Rent(Vector3 position, Quaternion rotation)
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = Addressables.InstantiateAsync(key, position, rotation).WaitForCompletion();
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

        public GameObject Rent(Vector3 position, Quaternion rotation, Transform parent)
        {
            ThrowIfDisposed();

            if (!stack.TryPop(out var obj))
            {
                obj = Addressables.InstantiateAsync(key, position, rotation, parent).WaitForCompletion();
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

        public void Prewarm(int count)
        {
            ThrowIfDisposed();

            for (int i = 0; i < count; i++)
            {
                var obj = Addressables.InstantiateAsync(key).WaitForCompletion();
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