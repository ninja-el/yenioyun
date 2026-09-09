using System.Collections.Generic;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Tüm runtime instance'ların tek sahibi. Gameplay içinde Instantiate/Destroy yerine Get/Release
    /// kullanılır. Havuz kökü DontDestroyOnLoad altındadır ve objeler level sahnesine parent edilmez;
    /// bu yüzden GameScene boşaltıldığında havuzdaki objeler yok olmaz.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private class Pool
        {
            public Pool(Transform container)
            {
                Container = container;
            }

            public Transform Container { get; }
            public Queue<GameObject> Available { get; } = new Queue<GameObject>();
        }

        private readonly Dictionary<GameObject, Pool> _pools = new Dictionary<GameObject, Pool>();
        private readonly Dictionary<GameObject, GameObject> _prefabByInstance = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<GameObject, IPoolable> _poolableByInstance = new Dictionary<GameObject, IPoolable>();
        private readonly HashSet<GameObject> _activeInstances = new HashSet<GameObject>();
        private readonly List<GameObject> _releaseBuffer = new List<GameObject>();

        private Transform _root;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _root = new GameObject("PoolRoot").transform;
            _root.SetParent(transform, false);
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        /// <summary>Havuzdan hazır bir instance verir; havuz boşsa yenisini üretir. Konumu çağıran ayarlar.</summary>
        public GameObject Get(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("PoolManager.Get was called with a null prefab.", this);
                return null;
            }

            Pool pool = GetOrCreatePool(prefab);
            GameObject instance = TakeAvailable(pool);

            if (instance == null)
            {
                instance = CreateInstance(prefab, pool);
            }

            instance.SetActive(true);
            _activeInstances.Add(instance);

            if (_poolableByInstance.TryGetValue(instance, out IPoolable poolable) && poolable != null)
            {
                poolable.OnSpawned();
            }

            return instance;
        }

        /// <summary>Instance'ı havuza iade eder. Havuza ait olmayan obje yok edilmez, uyarı basılır.</summary>
        public void Release(GameObject instance)
        {
            if (instance == null) { return; }

            if (!_prefabByInstance.TryGetValue(instance, out GameObject prefab))
            {
                Debug.LogWarning($"PoolManager.Release received an instance it does not own: {instance.name}", instance);
                return;
            }

            if (!_activeInstances.Remove(instance)) { return; }

            if (_poolableByInstance.TryGetValue(instance, out IPoolable poolable) && poolable != null)
            {
                poolable.OnDespawned();
            }

            instance.SetActive(false);
            instance.transform.SetParent(_pools[prefab].Container, false);
            _pools[prefab].Available.Enqueue(instance);
        }

        /// <summary>Aktif tüm instance'ları havuza iade eder. Level sahnesi boşaltılmadan önce çağrılır.</summary>
        public void ReleaseAll()
        {
            _releaseBuffer.Clear();
            _releaseBuffer.AddRange(_activeInstances);

            for (int i = 0; i < _releaseBuffer.Count; i++)
            {
                Release(_releaseBuffer[i]);
            }

            _releaseBuffer.Clear();
        }

        private Pool GetOrCreatePool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out Pool pool)) { return pool; }

            GameObject container = new GameObject(prefab.name);
            container.transform.SetParent(_root, false);

            pool = new Pool(container.transform);
            _pools.Add(prefab, pool);
            return pool;
        }

        private GameObject TakeAvailable(Pool pool)
        {
            // Dışarıdan Destroy edilmiş bir instance havuzda kalmış olabilir; ilk sağlam olanı al.
            while (pool.Available.Count > 0)
            {
                GameObject instance = pool.Available.Dequeue();
                if (instance != null) { return instance; }
            }

            return null;
        }

        private GameObject CreateInstance(GameObject prefab, Pool pool)
        {
            GameObject instance = Instantiate(prefab, pool.Container);
            instance.name = prefab.name;

            _prefabByInstance.Add(instance, prefab);
            _poolableByInstance.Add(instance, instance.GetComponent<IPoolable>());

            return instance;
        }
    }
}
