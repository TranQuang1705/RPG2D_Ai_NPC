using UnityEngine;


public class UIManager : Singleton<UIManager>
{
    private int openPanelCount = 0; 

    protected override void Awake()
    {
        base.Awake();
    }


    public void OnPanelOpened()
    {
        openPanelCount++;
        
        if (openPanelCount == 1) 
        {
            DisablePlayerControls();
        }
        
        Debug.Log($"📋 UIManager: Panel opened. Total open: {openPanelCount}");
    }

    public void OnPanelClosed()
    {
        openPanelCount--;
        
        if (openPanelCount < 0)
        {
            Debug.LogWarning("⚠️ UIManager: Panel count < 0, resetting to 0");
            openPanelCount = 0;
        }
        
        if (openPanelCount == 0) 
        {
            EnablePlayerControls();
        }
        
        Debug.Log($"📋 UIManager: Panel closed. Total open: {openPanelCount}");
    }


    public bool IsAnyPanelOpen()
    {
        return openPanelCount > 0;
    }

    private void DisablePlayerControls()
    {
        Debug.Log("🚫 Disabling player controls");
        
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetCanMove(false);
        }
        
        if (ActiveWeapon.Instance != null)
        {
            ActiveWeapon.Instance.SetCanAttack(false);
        }
    }

    private void EnablePlayerControls()
    {
        Debug.Log("✅ Enabling player controls");
        
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetCanMove(true);
        }
        
        if (ActiveWeapon.Instance != null)
        {
            ActiveWeapon.Instance.SetCanAttack(true);
        }

    }


    public void ForceCloseAllPanels()
    {
        Debug.LogWarning("⚠️ Force closing all panels!");
        openPanelCount = 0;
        EnablePlayerControls();
    }
}
