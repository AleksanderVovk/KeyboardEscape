using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderboardCore : MonoBehaviour
{
    [SerializeField] private BoardData[] _boardsData;

    private float timer;
    [SerializeField] private float _timeToUpdateLB;

    public void InitializationLeaderboard()
    {
        UpdateLeaderboard();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // ≈сли врем€ истекло, выбираем новую случайную точку
        if (timer >= _timeToUpdateLB)
        {
            UpdateLeaderboard();

            timer = 0;
        }
    }

    public void UpdateLeaderboard()
    {
        List<Player> players = PlayersManager.instance.allPlayers.OrderByDescending(item => item.score).ToList();

        // —начала деактивируем все доски
        foreach (BoardData boardData in _boardsData)
        {
            boardData.gameObject.SetActive(false);
        }

        // јктивируем только те доски, дл€ которых есть игроки и которые есть в массиве
        for (int i = 0; i < players.Count && i < _boardsData.Length; i++)
        {
            if (_boardsData[i] != null)
            {
                _boardsData[i].gameObject.SetActive(true);
                _boardsData[i].SetData(i + 1, players[i]);
            }
        }
    }
}
