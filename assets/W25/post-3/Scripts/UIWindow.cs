using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIWindow : MonoBehaviour
{
    public virtual void EnableUI()
    {
        UIManager.closeAllUI.AddListener(CloseUI);
    }

    public virtual void DisableUI()
    {
        UIManager.closeAllUI.RemoveListener(CloseUI);
    }

    public virtual void CloseUI()
    {
        UIManager.UIAction.Invoke();
        UIManager.Instance.CloseUI(gameObject);
    }
}
