using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

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

    /// <summary>
    /// Check if any input field is currently focused (being typed in)
    /// </summary>
    public bool IsInputFieldFocused()
    {
        GameObject selected = EventSystem.current?.currentSelectedGameObject;
        if (selected != null)
        {
            // Check for TMP_InputField
            TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
            if (tmpInput != null && tmpInput.isFocused)
            {
                return true;
            }
            
            // Check for legacy InputField (just in case)
            UnityEngine.UI.InputField legacyInput = selected.GetComponent<UnityEngine.UI.InputField>();
            if (legacyInput != null && legacyInput.isFocused)
            {
                return true;
            }
        }
        return false;
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
