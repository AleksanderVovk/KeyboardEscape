using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BoardData : MonoBehaviour
{
    [SerializeField] private TMP_Text _rank;
    [SerializeField] private TMP_Text _nickname;
    [SerializeField] private TMP_Text _score;

    [SerializeField] private Color[] _colorNickname; // 0 - Игрок, 1 - НПС

    public void SetData(int rank, Player player)
    {
        // Установка места в топе
        //_rank.text = rank.ToString();

        int nicknameColor = 0;

        // Установка никнейма
        if (player.thisRealPlayer)
        {
            nicknameColor = 0;

            _nickname.text = UTF.ReadLanguageDirectory(PlayersManager.instance.yourNickname);
        }
        else
        {
            _nickname.text = player.nickname;

            nicknameColor = 1;
        }

        _nickname.color = _colorNickname[nicknameColor];

        // Установка очков
        _score.text = player.score + "<size=65>" + UTF.manager.TextToSprite("\U0001F604") + "</size>";
    }
}
