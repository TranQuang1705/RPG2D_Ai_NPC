using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ClickDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ped = new PointerEventData(EventSystem.current)
            { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            Debug.Log($"[Raycast] {Input.mousePosition}");
            foreach (var r in results) Debug.Log($" - {r.gameObject.name}");
        }
    }
}
