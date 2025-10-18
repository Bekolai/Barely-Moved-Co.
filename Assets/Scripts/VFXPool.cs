using System.Collections.Generic;
using UnityEngine;

public enum VFXType
{
    Destroy,
    Hit,
    Explosion,
    Heal
    // add more as you go
}
public class VFXPool : MonoBehaviour
{
    public static VFXPool Instance { get; private set; }

    [System.Serializable]
    public class VFXPrefabEntry
    {
        public VFXType Type;
        public GameObject Prefab;
        public int InitialPoolSize = 5;
    }

    [Header("VFX Prefabs")]
    public List<VFXPrefabEntry> vfxPrefabs = new List<VFXPrefabEntry>();

    private Dictionary<VFXType, Queue<GameObject>> pool = new Dictionary<VFXType, Queue<GameObject>>();
    private Dictionary<VFXType, GameObject> prefabLookup = new Dictionary<VFXType, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    void InitializePool()
    {
        foreach (var entry in vfxPrefabs)
        {
            if (!pool.ContainsKey(entry.Type))
            {
                pool[entry.Type] = new Queue<GameObject>();
                prefabLookup[entry.Type] = entry.Prefab;

                for (int i = 0; i < entry.InitialPoolSize; i++)
                {
                    var obj = CreateNewInstance(entry.Type);
                    obj.SetActive(false);
                    pool[entry.Type].Enqueue(obj);
                }
            }
        }
    }

    GameObject CreateNewInstance(VFXType type)
    {
        var prefab = prefabLookup[type];
        var obj = Instantiate(prefab, transform);
        return obj;
    }

    public void Play(VFXType type, Vector3 position, Quaternion rotation, Vector3 scale, float autoReturnTime = -1)
    {
        if (!pool.ContainsKey(type))
        {
            Debug.LogWarning($"No VFX type registered for: {type}");
            return;
        }

        GameObject obj;
        if (pool[type].Count > 0)
        {
            obj = pool[type].Dequeue();
        }
        else
        {
            obj = CreateNewInstance(type);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.localScale = scale;
        obj.SetActive(true);

        var vfxObj = obj.GetComponent<VFXObject>();
        Debug.Log($"Playing VFX: {type} {vfxObj.Particle}");
        vfxObj.Particle?.Play();

        if (autoReturnTime > 0)
        {
            Instance.StartCoroutine(ReturnAfterDelay(type, obj, autoReturnTime));
        }
        else
        {
            Instance.StartCoroutine(ReturnAfterParticleEnds(type, vfxObj));
        }
    }

    System.Collections.IEnumerator ReturnAfterParticleEnds(VFXType type, VFXObject vfxObj)
    {
        var ps = vfxObj.Particle;
        yield return new WaitUntil(() => !ps.IsAlive(true));
        ReturnToPool(type, vfxObj.gameObject);
    }

    System.Collections.IEnumerator ReturnAfterDelay(VFXType type, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(type, obj);
    }

    void ReturnToPool(VFXType type, GameObject obj)
    {
        obj.SetActive(false);
        pool[type].Enqueue(obj);
    }
}
