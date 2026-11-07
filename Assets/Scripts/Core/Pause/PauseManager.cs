using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    public bool isPause;
    public bool isAdPause;

    // Ссылки
    [SerializeField] private WorldCharacteristic _worldCharacteristics;

    private Coroutine inputCheckCoroutine;

    private void Awake()
    {
        instance = this;
    }

    public static void Pause(bool state) => instance._Pause(state);
    private void _Pause(bool state)
    {
        if (state)
            PageManager.OpenPageByName("Settings");
        else
            PageManager.CloseAllPages();

        PauseMethod(state);
    }

    private void StartInputChecking()
    {
        if (inputCheckCoroutine == null)
        {
            inputCheckCoroutine = StartCoroutine(CheckInputDuringPause());
        }
    }

    private void StopInputChecking()
    {
        if (inputCheckCoroutine != null)
        {
            StopCoroutine(inputCheckCoroutine);
            inputCheckCoroutine = null;
        }
    }

    private IEnumerator CheckInputDuringPause()
    {
        while (isAdPause)
        {
            // Проверяем ввод каждый кадр (несмотря на timeScale = 0)
            if (CheckForUnpauseInput())
            {
                ForwardPauseAd();
                yield break; // Прерываем корутину при отключении паузы
            }

            yield return null; // Продолжаем проверять каждый кадр
        }
    }

    private void OnDisable()
    {
        StopInputChecking();
    }

    private bool CheckForUnpauseInput()
    {
        // Проверка движения
        Vector3 inputs = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0,
            Input.GetAxisRaw("Vertical")
        );

        // Проверка клавиши пробела
        if (inputs.magnitude > 0 || Input.GetKeyDown(KeyCode.Space))
        {
            return true;
        }

        return false;
    }

    public void PauseAd(bool state)
    {
        if (state)
            PageManager.OpenPageByName("AdPause");
        else
            PageManager.CloseAllPages();

        isAdPause = state;

        PauseMethod(state);

        // Управление корутиной проверки ввода
        if (state)
        {
            StartInputChecking();
        }
        else
        {
            StopInputChecking();
        }
    }

    public void ForwardPauseAd()
    {
        PageManager.CloseAllPages();

        isAdPause = false;

        PauseMethod(false);

        StopInputChecking();
    }

    public static void PauseWithoutInterface(bool state) => instance._PauseWithoutInterface(state);
    public void _PauseWithoutInterface(bool state)
    {
        PauseMethod(state);
    }

    private void PauseMethod(bool state)
    {
        isPause = state;

        if (state)
        {
            Time.timeScale = 0f;
            AudioListener.volume = 0f;
        }
        else
        {
            Time.timeScale = _worldCharacteristics.WORLD_TIMESCALE_CURRENT;
            AudioListener.volume = YG2.saves.settings.volume;
        }

        Debug.Log("Состояние паузы: " + state);
    }
}
