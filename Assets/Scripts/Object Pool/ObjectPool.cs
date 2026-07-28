using System.Collections.Generic;
using UnityEngine;

namespace Valley.Core.Pooling
{
    public class ObjectPool<T> where T : Component
    {
        readonly T prefab;
        readonly Transform parent;
        readonly Stack<T> inactive = new Stack<T>();

        public ObjectPool(T prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public T Get()
        {
            T instance = inactive.Count > 0 ? inactive.Pop() : Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            inactive.Push(instance);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T instance = Object.Instantiate(prefab, parent);
                instance.gameObject.SetActive(false);
                inactive.Push(instance);
            }
        }
    }

    public class PrefabPoolGroup<T> where T : Component
    {
        readonly Transform parent;
        readonly Dictionary<T, ObjectPool<T>> poolsByPrefab = new Dictionary<T, ObjectPool<T>>();
        readonly Dictionary<T, T> prefabByInstance = new Dictionary<T, T>();

        public PrefabPoolGroup(Transform parent)
        {
            this.parent = parent;
        }

        public T Get(T prefab)
        {
            if (!poolsByPrefab.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<T>(prefab, parent);
                poolsByPrefab[prefab] = pool;
            }

            T instance = pool.Get();
            prefabByInstance[instance] = prefab;
            return instance;
        }

        public void Release(T instance)
        {
            if (!prefabByInstance.TryGetValue(instance, out var prefab)) return;
            if (poolsByPrefab.TryGetValue(prefab, out var pool)) pool.Release(instance);
        }
    }
}