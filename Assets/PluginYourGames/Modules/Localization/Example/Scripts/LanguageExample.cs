using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YG.Example
{
    public class LanguageExample : MonoBehaviour
    {
        public string ru, en, tr;

        private TMP_Text textComponent;

        public bool addStrengthSprite;

        private void Awake()
        {
            if (addStrengthSprite == false)
            {
                textComponent = GetComponent<TMP_Text>();
                SwitchLanguage(YG2.lang);
            }
        }

        private void Start()
        {
            if (addStrengthSprite == true)
            {
                textComponent = GetComponent<TMP_Text>();
                SwitchLanguage(YG2.lang);
            }
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += SwitchLanguage;
        }
        private void OnDisable()
        {
            YG2.onSwitchLang -= SwitchLanguage;
        }

        public void SwitchLanguage(string lang)
        {
            string _text = "";

            switch (lang)
            {
                case "ru":
                        _text = ru;
                    break;
                case "tr":
                        _text = tr;
                    break;
                default:
                        _text = en;
                    break;
            }

            if (addStrengthSprite)
                textComponent.text = ru + UTF.manager.TextToSprite("\U0001F602");
            else
                textComponent.text = _text;
        }
    }
}