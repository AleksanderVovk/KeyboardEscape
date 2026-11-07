using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersManager : MonoBehaviour
{
    public static PlayersManager instance;

    public MeshRenderer spawnBounds;

    public Transform sellPoint;

    public Player realPlayer;
    public Player[] allPlayers;

    public string[] playersNicknames;

    public LanguageDirectory yourNickname;

    public LeaderboardCore[] leaderboardsCore;

    public Base[] allBases;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (allPlayers.Length <= 0)
            Debug.Log("Вы не добавили игроков в {PlayersManager}");
    }

    public void ReadNames()
    {
        return;

        // Загружаем текстовый файл из папки Resources
        TextAsset textAsset = Resources.Load<TextAsset>("names"); // Указываем имя файла без расширения

        if (textAsset != null)
        {
            // Разделяем текст на строки и сохраняем в массив
            playersNicknames = textAsset.text.Split('\n');
        }
        else
        {
            Debug.LogError("Файл не найден!");
        }
    }

    public static string GetRandomNickName() => instance._GetRandomNickName();
    public string _GetRandomNickName()
    {
        return playersNicknames[Random.Range(0, playersNicknames.Length)];
    }

    public static Vector3 GetRealPlayerPosition() => instance._GetRealPlayerPosition();
    public Vector3 _GetRealPlayerPosition()
    {
        return realPlayer.transform.position;
    }

    public void InitializationAllPlayers()
    {
        foreach (Player player in allPlayers)
        {
            if (player.thisRealPlayer)
            {
                realPlayer = player;
                player.InitializationPlayer();
            }
            else
            {
                player.InitializationNPC();
            }
        }

        InitializePlayersAndAssignBases();

        for (int i = 0; i < leaderboardsCore.Length; i++)
            leaderboardsCore[i].InitializationLeaderboard();
    }

    public void InitializePlayersAndAssignBases()
    {
        if (allPlayers == null || allBases == null)
        {
            Debug.LogError("allPlayers/allBases is null");
            return;
        }

        if (allPlayers.Length != allBases.Length)
        {
            Debug.LogError($"Нужно одинаковое количество: players={allPlayers.Length}, bases={allBases.Length}");
            return;
        }

        // 1) Копия баз, чтобы не портить порядок в инспекторе
        Base[] shuffledBases = (Base[])allBases.Clone();

        // 2) Перемешали без повторений
        Shuffle(shuffledBases);

        // 3) Инициализация + выдача базы
        for (int i = 0; i < allPlayers.Length; i++)
        {
            Player p = allPlayers[i];
            Base b = shuffledBases[i];

            // твоя логика инициализации
            if (p.thisRealPlayer)
            {
                realPlayer = p;
                p.InitializationPlayer();
            }
            else
            {
                p.InitializationNPC();
            }

            // главное: назначаем уникальную базу
            p.InitializationBase(b);
        }
    }

    // Fisher–Yates shuffle [web:35]
    private static void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    public static Player[] GetAllPlayers() => instance._GetAllPlayers();
    public Player[] _GetAllPlayers()
    {
        return allPlayers;
    }
}
