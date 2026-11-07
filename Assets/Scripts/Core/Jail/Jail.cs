using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YG;

public class Jail : MonoBehaviour
{
    public static Jail manager;

    public int currentScore;
    public int maxScore;

    public Transform[] points;

    public LanguageDirectory pickupText;

    [SerializeField] private Slider _sliderProgress;
    [SerializeField] private TMP_Text _textProgress;
    [SerializeField] private LanguageDirectory _progressText;

    [SerializeField] private MessageView _timer;
    private float totalTime = 0f;
    private float lastUpdateTime = 0f;
    private bool isTimerRunning = false;

    [SerializeField] private TMP_Text _currentTimerFinish;
    [SerializeField] private LanguageDirectory _currentTimerFinish_prefix;

    [SerializeField] private TMP_Text _bestTimerFinish;
    [SerializeField] private LanguageDirectory _bestTimerFinish_prefix;

    [SerializeField] private AdvManager _advManager;

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        _sliderProgress.maxValue = maxScore;
        UpdateProgress();

        StartTimer();
    }

    public void Finish()
    {
        _advManager.enabled = false;
        PlayersManager.instance.realPlayer.playerState.canMove = false;
        PageManager.OpenPageByName("FinishPage");

        YG2.MetricaSend("Finish");

        if (totalTime > YG2.saves.playerData.bestTime)
        {
            YG2.saves.playerData.bestTime = totalTime;

            YG2.SetLBTimeConvert("TopTime", YG2.saves.playerData.bestTime);

            YG2.SaveProgress();
        }

        _currentTimerFinish.text = UTF.ReadLanguageDirectory(_currentTimerFinish_prefix) + "" + GetTime(totalTime);
        _bestTimerFinish.text = UTF.ReadLanguageDirectory(_bestTimerFinish_prefix) + "" + GetTime(YG2.saves.playerData.bestTime);

        UISound.manager.PlayWin(false);
    }

    public void AddScore(int score)
    {
        currentScore += score;
        UpdateProgress();

        if (currentScore >= maxScore)
        {
            Finish();
            StopTimer();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UpdateProgress()
    {
        _sliderProgress.value = currentScore;
        _textProgress.text = UTF.ReadLanguageDirectory(_progressText) + "" + currentScore + " / " + maxScore;
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            totalTime += Time.deltaTime;

            // Обновляем отображение каждую секунду
            if (totalTime - lastUpdateTime >= 1f)
            {
                lastUpdateTime = Mathf.Floor(totalTime);
                UpdateTimerDisplay();
            }
        }
    }

    // Метод для старта таймера
    public void StartTimer()
    {
        _timer.Show(0);
        isTimerRunning = true;
        totalTime = 0f;
        lastUpdateTime = 0f;
        UpdateTimerDisplay(); // Обновляем текст сразу при старте
    }

    // Метод для остановки таймера
    public void StopTimer()
    {
        isTimerRunning = false;
        // Фиксируем окончательное время
        UpdateTimerDisplay();
    }

    // Метод для получения текущего времени в секундах
    public float GetCurrentTime()
    {
        return totalTime;
    }

    // Обновление отображения таймера
    private void UpdateTimerDisplay()
    {
        if (_timer != null)
        {
            _timer.SetMessage(GetTime(totalTime));
        }
    }

    public string GetTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public static void SetToJail(GameObject go) => manager._SetToJail(go);
    private void _SetToJail(GameObject go)
    {
        if (go == null || points == null || points.Length == 0)
            return;

        // Собираем все свободные точки (без дочерних объектов)
        List<Transform> freePoints = new List<Transform>();
        foreach (Transform point in points)
        {
            if (point.childCount == 0)
            {
                freePoints.Add(point);
            }
        }

        // Если есть свободные точки
        if (freePoints.Count > 0)
        {
            // Выбираем случайную свободную точку
            Transform randomPoint = freePoints[Random.Range(0, freePoints.Count)];

            // Перемещаем объект в позицию точки
            go.transform.position = randomPoint.position;
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // Делаем объект дочерним к точке
            go.transform.SetParent(randomPoint);

            AddScore(1);
            UISound.manager.PlayHit(true);
        }
        else
        {
            Debug.LogWarning("No free jail points available!");
        }
    }
}
