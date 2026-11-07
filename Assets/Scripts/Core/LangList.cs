using UnityEngine;

public class LangList : MonoBehaviour
{
    public static LangList instance;

    // Core
    public LanguageDirectory second;
    public LanguageDirectory notEnoughMoney;
    public LanguageDirectory money;

    // Brainrot state
    public LanguageDirectory pickupBrainrot;
    public LanguageDirectory dropBrainrot;
    public LanguageDirectory sellBrainrot;

    // Notification
    public LanguageDirectory handAreBusy;
    public LanguageDirectory noFreeBrainrotPoint;

    // Stats
    public LanguageDirectory speed;
    public LanguageDirectory carry;
    public LanguageDirectory rebirths;

    // Shop
    public LanguageDirectory buy;
    public LanguageDirectory equip;
    public LanguageDirectory equiped;

    // Gift
    public LanguageDirectory receive;

    public LanguageDirectory level;

    public LanguageDirectory step;
    public LanguageDirectory required;

    public LanguageDirectory wins;
    public LanguageDirectory win;
    public LanguageDirectory won;

    private void Awake()
    {
        instance = this;
    }
}
