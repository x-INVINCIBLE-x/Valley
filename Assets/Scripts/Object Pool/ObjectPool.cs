using System.Collections.Generic;
using UnityEngine;

namespace Valley.Core.Pooling
{
    public class ObjectPool<T> where T : Component
    {
        readonly T prefab;
        readonly Transform parent;
        readonly Vector3 prefabWorldScale;
        readonly Stack<T> inactive = new Stack<T>();

        public ObjectPool(T prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
            prefabWorldScale = prefab.transform.localScale;
        }

        public T Get()
        {
            T instance = inactive.Count > 0 ? inactive.Pop() : Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(true);
            ApplyUnscaledParenting(instance.transform);
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
                ApplyUnscaledParenting(instance.transform);
                instance.gameObject.SetActive(false);
                inactive.Push(instance);
            }
        }

        void ApplyUnscaledParenting(Transform instanceTransform)
        {
            Vector3 parentLossyScale = parent != null ? parent.lossyScale : Vector3.one;

            instanceTransform.localScale = new Vector3(
                Mathf.Approximately(parentLossyScale.x, 0f) ? prefabWorldScale.x : prefabWorldScale.x / parentLossyScale.x,
                Mathf.Approximately(parentLossyScale.y, 0f) ? prefabWorldScale.y : prefabWorldScale.y / parentLossyScale.y,
                Mathf.Approximately(parentLossyScale.z, 0f) ? prefabWorldScale.z : prefabWorldScale.z / parentLossyScale.z);
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