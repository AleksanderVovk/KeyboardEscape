using TMPro;
using UnityEngine;

public class HeadInfo : MonoBehaviour 
{
    [SerializeField] private TMP_Text _timerToDestroy;
    [SerializeField] private TMP_Text _brainrotName;
    [SerializeField] private TMP_Text _brainrotRare;
    public TMP_Text mutationDisplay;
    [SerializeField] private TMP_Text _earningPerSecond;

    private string _second;

    private void Awake()
    {
        _second = UTF.ReadLanguageDirectory(LangList.instance.second);
    }

    public void SetData(BrainrotBase brainrot)
    {
        if (brainrot == null) return;

        _brainrotName.text = UTF.ReadLanguageDirectory(brainrot.brainrotName);

        _brainrotRare.text = BrainrotManager.GetBrainrotRareName(brainrot.brainrotRare);
        _brainrotRare.color = BrainrotManager.GetBrainrotRareColor(brainrot.brainrotRare);

        _earningPerSecond.gameObject.SetActive(brainrot.earningPerSecond > 0);
        _earningPerSecond.text = "$" + brainrot.earningPerSecond + "/" + _second;
    }

    public void UpdateTimer(int secondsLeft)
    {
        if (_timerToDestroy == null) return;
        _timerToDestroy.text = secondsLeft + _second;
    }

    public void SetTimerVisible(bool visible)
    {
        if (_timerToDestroy != null)
            _timerToDestroy.gameObject.SetActive(visible);
    }
}
