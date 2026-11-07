using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHelper : MonoBehaviour
{
    public static UIHelper instance;

    [SerializeField] private Sprite _errorIcon;

    private void Awake()
    {
        instance = this;
    }

    public static Sprite GetErrorIcon() => instance._GetErrorIcon();
    public Sprite _GetErrorIcon()
    {
        return _errorIcon;
    }
}
