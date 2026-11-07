using System;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

#if TMP_YG2
using TMPro;
#endif

namespace YG
{
    public class LBPlayerDataYG : MonoBehaviour
    {
        public ImageLoadYG imageLoad;

        [Serializable]
        public struct TextLegasy
        {
            public TMP_Text rank, name, score;
        }
        public TextLegasy textLegasy;

#if TMP_YG2
        [Serializable]
        public struct TextMP
        {
            public TextMeshProUGUI rank, name, score;
        }
        public TextMP textMP;
#endif
        [Space(10)]
        public MonoBehaviour[] topPlayerActivityComponents = new MonoBehaviour[0];
        public MonoBehaviour[] currentPlayerActivityComponents = new MonoBehaviour[0];

        public Color[] tops;
        public GameObject thisCurrentPlayerObj;
        public GameObject top1Obj;
        public bool isCoinUTF;
        public bool isWinUTF;
        public bool isEggUTF;

        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite _topBackground;
        [SerializeField] private Sprite _defaultBackground;

        [SerializeField] private Color _top1backgroundColor;
        [SerializeField] private Color _top2backgroundColor;
        [SerializeField] private Color _top3backgroundColor;

        [SerializeField] private Color _playerBackgroundColor;

        public class Data
        {
            public string rank;
            public string name;
            public string score;
            public string photoUrl;
            public bool inTop;
            public bool currentPlayer;
            public Sprite photoSprite;
        }

        [HideInInspector]
        public Data data = new Data();

        public void UpdateEntries()
        {
            if (textLegasy.rank && data.rank != null) textLegasy.rank.text = data.rank.ToString();
            if (textLegasy.name && data.name != null) textLegasy.name.text = data.name;
            if (isCoinUTF || isWinUTF || isEggUTF)
            {
                if (isCoinUTF)
                {
                    if (textLegasy.score && data.score != null) textLegasy.score.text = data.score.ToString() + TextToSprite("\U0001F601");
                }
                else if (isWinUTF)
                {
                    if (textLegasy.score && data.score != null) textLegasy.score.text = data.score.ToString() + TextToSprite("\U0001F602");
                }
                else if (isEggUTF)
                {
                    if (textLegasy.score && data.score != null) textLegasy.score.text = data.score.ToString() + TextToSprite("\U0001F604");
                }
            }
            else
            {
                if (textLegasy.score && data.score != null) textLegasy.score.text = data.score.ToString();
            }

#if TMP_YG2
            if (textMP.rank && data.rank != null) textMP.rank.text = data.rank.ToString();
            if (textMP.name && data.name != null) textMP.name.text = data.name;
            if (textMP.score && data.score != null) textMP.score.text = data.score.ToString();
#endif
            if (imageLoad)
            {
                if (data.photoSprite)
                {
                    imageLoad.SetTexture(data.photoSprite.texture);
                }
                else if (data.photoUrl == null)
                {
                    imageLoad.ClearTexture();
                }
                else
                {
                    imageLoad.Load(data.photoUrl);
                }
            }

            if (topPlayerActivityComponents.Length > 0)
            {
                if (data.inTop)
                {
                    ActivityMomoObjects(topPlayerActivityComponents, true);
                }
                else
                {
                    ActivityMomoObjects(topPlayerActivityComponents, false);
                }
            }

            if (currentPlayerActivityComponents.Length > 0)
            {
                if (data.currentPlayer)
                {
                    ActivityMomoObjects(currentPlayerActivityComponents, true);
                }
                else
                {
                    ActivityMomoObjects(currentPlayerActivityComponents, false);
                }
            }

            void ActivityMomoObjects(MonoBehaviour[] objects, bool activity)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    objects[i].enabled = activity;
                }
            }

            _backgroundImage.sprite = _defaultBackground;

            if (data.rank == "1")
            {
                //top1Obj.SetActive(true);
                _backgroundImage.sprite = _topBackground;
                _backgroundImage.color = _top1backgroundColor;
            }
            else if (data.rank == "2")
            {
                _backgroundImage.sprite = _topBackground;
                _backgroundImage.color = _top2backgroundColor;
            }
            else if (data.rank == "3")
            {
                _backgroundImage.sprite = _topBackground;
                _backgroundImage.color = _top3backgroundColor;
            }

            if (data.currentPlayer)
            {
                thisCurrentPlayerObj.SetActive(true);
                _backgroundImage.sprite = _topBackground;

                _backgroundImage.color = _playerBackgroundColor;
            }
        }

        public string TextToSprite(string input)
        {
            var output = input;

            Regex pattern = new Regex(@"\\u[a-zA-Z0-9]*\\u[a-zA-Z0-9]*");

            while (output.Contains(@"\u"))
            {
                output = pattern.Replace(output,
                    m => {
                        var pair = m.Value;
                        var first = pair.Substring(0, 6);
                        var second = pair.Substring(6, 6);
                        var firstInt = Convert.ToInt32(first.Substring(2), 16);
                        var secondInt = Convert.ToInt32(second.Substring(2), 16);
                        var codePoint = (firstInt - 0xD800) * 0x400 + (secondInt - 0xDC00) + 0x10000;
                        return @"\U" + codePoint.ToString("X8");
                    },
                    1
                );
            }

            return output;
        }

    }
}