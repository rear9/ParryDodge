using System.Collections.Generic;
using UnityEngine;

public class AttackPoolManager : MonoBehaviour
{
    public static AttackPoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string key; // assigns id to each attack in a pool
        public GameObject prefab; // prefab to instantiate
        public int initialSize = 5; // size of the pool
    }

    [SerializeField] private List<Pool> pools = new(); // list + dictionary creation
    private readonly List<GameObject> activeAttacksList = new();
    private readonly Dictionary<string, Queue<GameObject>> _poolDict = new();
    private readonly Dictionary<string, Transform> _poolParentDict = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        foreach (var pool in pools) // for each type of attack, create a parent folder
        {
            var objectPool = new Queue<GameObject>();
            GameObject folder = new GameObject(pool.key + "_Pool");
            folder.transform.SetParent(transform);
            _poolParentDict[pool.key] = folder.transform;
            
            for (int i = 0; i < pool.initialSize; i++) // for each parent folder, instantiate attack prefabs equal to the size of the pool
            {
                var obj = Instantiate(pool.prefab, Vector3.zero, Quaternion.identity, folder.transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            _poolDict[pool.key] = objectPool;
        }
    }

    public GameObject SpawnFromPool(string key, Vector3 pos, Quaternion rot)
    {
        if (!_poolDict.TryGetValue(key, out var queue) || queue.Count == 0)
        {
            Debug.LogWarning($"{key} is empty");
            return null;
        }
        
        var spawnObj = _poolDict[key].Dequeue(); // pull attack prefab out of the pool folder into play area and activate it
        spawnObj.transform.SetParent(null);
        if (spawnObj.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = pos;
            rb.rotation = rot.eulerAngles.z;
        }
        spawnObj.transform.SetPositionAndRotation(pos,rot);
        spawnObj.SetActive(true);
        activeAttacksList.Add(spawnObj);
        return spawnObj;
    }
    public void ReturnToPool(string key, GameObject obj)
    {
        if (!_poolDict.TryGetValue(key, out var val))
        {
            Destroy(obj); // fallback
        }

        obj.SetActive(false); // reverse the above function
        activeAttacksList.Remove(obj);
        obj.transform.SetParent(_poolParentDict[key]);
        val.Enqueue(obj);
    }
    
    public void ReturnAllToPool() // return all active objects to pool using the activeAttacksList (necessary to clean-up on player death)
    {
        var copy = new List<GameObject>(activeAttacksList);
        foreach (var obj in copy)
        {
            if (obj.TryGetComponent(out EnemyAttackCore atk))
            {
                if (!string.IsNullOrEmpty(atk.GetPoolKey()))
                    ReturnToPool(atk.GetPoolKey(), obj);
            }
            else
            {
                obj.SetActive(false);
            }
        }
        activeAttacksList.Clear();
    }
    public List<GameObject> GetActiveAttacks() // helper to get active attacks, used for custom attack logic
    {
        return new List<GameObject>(activeAttacksList);
    }

    
}