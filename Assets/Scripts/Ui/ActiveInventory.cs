using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInventory : Singleton<ActiveInventory>
{
    private int activeSlotIndexNum = 0;

    private PlayerControls playerControls;

    protected override void Awake()
    {
        base.Awake();
        playerControls = new PlayerControls();
    }

    private void Start()
    {
        playerControls.Inventory.Keyboard.performed += ctx => ToggleActiveSlot((int)ctx.ReadValue<float>());
    }


    private void OnEnable()
    {
        playerControls.Enable();
    }
    public void EquipStartingWeapon()
    {
        ToggleActiveHighlight(0);
    }
    private void ToggleActiveSlot(int numValue)
    {
        ToggleActiveHighlight(numValue - 1);
    }

    private void ToggleActiveHighlight(int indexNum)
    {
        activeSlotIndexNum = indexNum;

        foreach (Transform inventorySlot in this.transform)
        {
            inventorySlot.GetChild(0).gameObject.SetActive(false);
        }

        this.transform.GetChild(indexNum).GetChild(0).gameObject.SetActive(true);

        ChangeActiveWeapon();
    }

    private void ChangeActiveWeapon()
    {
        // ✅ Check if ActiveWeapon Instance exists
        if (ActiveWeapon.Instance == null)
        {
            Debug.LogError("❌ ActiveWeapon.Instance is null! Cannot change weapon.");
            return;
        }
        
        // Destroy current weapon if exists
        if (ActiveWeapon.Instance.CurrentActiveWeapon != null) 
        {
            Destroy(ActiveWeapon.Instance.CurrentActiveWeapon.gameObject);
        }

        Transform childTransform = transform.GetChild(activeSlotIndexNum);
        InventorySlot inventorySlot = childTransform.GetComponent<InventorySlot>();
        WeaponInfo weaponInfo = inventorySlot.GetWeaponInfo();

        if (weaponInfo == null)
        {
            ActiveWeapon.Instance.WeaponNull();
            return;
        }
        
        if (transform.GetChild(activeSlotIndexNum).GetComponentInChildren<InventorySlot>().GetWeaponInfo() == null)
        {
            ActiveWeapon.Instance.WeaponNull();
            return;
        }
        
        GameObject weaponToSpawn = transform.GetChild(activeSlotIndexNum).
        GetComponentInChildren<InventorySlot>().GetWeaponInfo().weaponPrefab;

        GameObject newWeapon = Instantiate(weaponToSpawn, ActiveWeapon.Instance.transform);
        
       // ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
       // newWeapon.transform.parent = ActiveWeapon.Instance.transform;   
        ActiveWeapon.Instance.NewWeapon(newWeapon.GetComponent<MonoBehaviour>());

    }

}
