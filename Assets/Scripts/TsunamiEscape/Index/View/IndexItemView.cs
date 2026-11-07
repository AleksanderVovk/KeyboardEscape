using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IndexItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Outline outline;
    [SerializeField] private TMP_Text rareName;
    [SerializeField] private TMP_Text mutationName;

    public void Bind(Brainrot brainrot, Mutation mutation, bool discovered)
    {
        rareName.text = BrainrotManager.GetBrainrotRareName(brainrot.brainrotRare);
        rareName.color = BrainrotManager.GetBrainrotRareColor(brainrot.brainrotRare);

        if (discovered)
        {
            if (mutation != null)
            {
                mutationName.text = UTF.ReadLanguageDirectory(mutation.mutationName);

                if (mutation.useGradient)
                {
                    mutationName.enableVertexGradient = true;
                    mutationName.colorGradientPreset = mutation.gradientPreset;
                }
                else
                {
                    mutationName.color = mutation.color;
                }
            }
            else
            {
                mutationName.text = "";
            }
        }
        else
        {
            mutationName.text = "???";
        }

        iconImage.sprite = brainrot.icon;
        iconImage.color = discovered ? Color.white : Color.black;

        // обводка под мутацию
        if (outline != null)
        {
            if (mutation != null)
            {
                outline.enabled = true;
                outline.effectColor = mutation.color; // если хочешь градиент Ч отдельна€ логика
            }
            else
            {
                outline.enabled = false;
            }
        }
    }
}
