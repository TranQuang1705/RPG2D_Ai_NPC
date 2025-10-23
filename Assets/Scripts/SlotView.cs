// using UnityEngine;
// using UnityEngine.EventSystems;

// public class SlotView : MonoBehaviour, IPointerEnterHandler
// {
//     public int Index { get; private set; }
//     private InventorySelection owner;

//     public void Bind(InventorySelection o, int idx)
//     {
//         owner = o;
//         Index = idx;
//     }

//     public void OnPointerEnter(PointerEventData eventData)
//     {
//         owner?.Select(Index);
//     }
// }
