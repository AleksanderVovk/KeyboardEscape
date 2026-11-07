using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebirthRewardItemUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _title;

    public void Set(Sprite icon, string title)
    {
        if (_icon) _icon.sprite = icon;
        if (_title) _title.text = title;
    }
}
