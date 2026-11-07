using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class DailyRewardCard : MonoBehaviour
{
    [SerializeField] private Image _tick;
    [SerializeField] private Image _lockIcon;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _amountText;

    private int _index;
    private bool _isClaimed;
    private bool _isUnlocked;

    public void Initialize(int index, TreasuresTypeGift type, int amount = 0)
    {
        _index = index;
        _isClaimed = YG2.saves.WasRewardCollectedOnSpecificDay[index];
        _isUnlocked = index < YG2.saves.DaysUnlocked;

        // ✅ Проверяем null ссылки!
        if (_tick == null) Debug.LogError($"Tick is NULL on card {index}!");
        if (_lockIcon == null) Debug.LogError($"LockIcon is NULL on card {index}!");

        UpdateVisualState();
    }


    private void UpdateVisualState()
    {
        Debug.Log($"Card {_index}: claimed={_isClaimed}, unlocked={_isUnlocked}");

        _tick.gameObject.SetActive(_isClaimed);
        _lockIcon.gameObject.SetActive(!_isUnlocked && !_isClaimed);

        Debug.Log($"Tick enabled: {_tick.enabled}, Lock enabled: {_lockIcon.enabled}");
    }


    public void MarkAsClaimed()
    {
        _isClaimed = true;
        YG2.saves.WasRewardCollectedOnSpecificDay[_index] = true;
        UpdateVisualState();
    }
}
