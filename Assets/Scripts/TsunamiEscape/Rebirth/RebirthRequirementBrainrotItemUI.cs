using UnityEngine;
using UnityEngine.UI;

public class RebirthRequirementBrainrotItemUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _checkmark;

    public void Set(Sprite icon, bool done)
    {
        if (_icon) _icon.sprite = icon;
        if (_checkmark) _checkmark.SetActive(done);
    }
}
