using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uPools
{
    public sealed class GameObjectPool : IObjectPool<GameObject>
    {
        public GameObjectPool(GameObject original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            this.original = original;
        }

        readonly GameObject original;
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
                obj = UnityEngine.Object.Instantiate(original);
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
                obj = UnityEngine.Object.Instantiate(original, parent);
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
                obj = UnityEngine.Object.Instantiate(original, position, rotation);
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
                obj = UnityEngine.Object.Instantiate(original, position, rotation, parent);
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
                UnityEngine.Object.Destroy(obj);
            }
            
            // Clean up any remaining rented items
            foreach (var rentedObj in rentedItems.ToArray())
            {
                UnityEngine.Object.Destroy(rentedObj);
            }
            
            rentedItems.Clear();
            allManagedObjects.Clear();
        }

        public void Prewarm(int count)
        {
            ThrowIfDisposed();

            for (int i = 0; i < count; i++)
            {
                var obj = UnityEngine.Object.Instantiate(original);
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