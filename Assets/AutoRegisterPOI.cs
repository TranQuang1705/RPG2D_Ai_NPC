using UnityEngine;

[RequireComponent(typeof(POIAnchor))]
public class AutoRegisterPOI : MonoBehaviour
{
    void Start()
    {
        var anchor = GetComponent<POIAnchor>();
        if (POIRegistry.I != null && anchor != null)
        {
            POIRegistry.I.Register(anchor.poiId, transform);
            Debug.Log($"[POIRegistry] Registered {anchor.poiId} at {transform.position}");
        }
        else
        {
            Debug.LogWarning("[POIRegistry] Registry instance not found!");
        }
    }

    void OnDestroy()
    {
        var anchor = GetComponent<POIAnchor>();
        if (POIRegistry.I != null && anchor != null)
            POIRegistry.I.Unregister(anchor.poiId, transform);
    }
}
