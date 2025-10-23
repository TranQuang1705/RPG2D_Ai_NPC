// POIRegistry.cs
using UnityEngine;
using System.Collections.Generic;

public class POIRegistry : MonoBehaviour
{
    public static POIRegistry I { get; private set; }
    private Dictionary<string, Transform> map = new();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(string id, Transform t) => map[id] = t;
    public void Unregister(string id, Transform t) { if (map.TryGetValue(id, out var cur) && cur == t) map.Remove(id); }
    public bool TryGet(string id, out Transform t) => map.TryGetValue(id, out t);
}
