using System.Collections;
using TMPro;
using UnityEngine;
using YG;

public class AdvManager : MonoBehaviour
{
    public float timerBeforeAdvertising; // Таймер до рекламы

    private float _lastAdvDelay;
    private float _timeTime;

    private Coroutine advProcess;
    [SerializeField] private GameObject _advPanel;
    public TMP_Text advText;
    private float startTime;
    private void Start()
    {
        _lastAdvDelay = 0f;
        _timeTime = 0;
        startTime = Time.time;
    }

    public void Update()
    {
        _timeTime = Time.time - startTime;

        if (timerBeforeAdvertising + _lastAdvDelay < _timeTime)
        {
            print("Показываем рекламу: ");
            StartCoroutine(advShowFunction());

            _lastAdvDelay = _timeTime;
        }
    }

    private IEnumerator advShowFunction()
    {
        switch (YG2.lang)
        {
            case "ru":
                advText.text = "Реклама будет показана через " + "<color=yellow>" + "2" + "</color>" + " секунды";
                break;
            case "en":
                advText.text = "The ad will be displayed in " + "<color=yellow>" + "2" + "</color>" + " seconds";
                break;
            case "tr":
                advText.text = "Reklam " + "<color=yellow>" + "2" + "</color>" + " saniye sonra gosterilecek";
                break;
        }

        _advPanel.gameObject.SetActive(true);

        yield return null;

        if (advProcess != null)
        {
            StopCoroutine(advProcess);
        }

        // Запускаем новую корутину таймера при паузе 
        advProcess = StartCoroutine(AdvProcess());
    }

    private IEnumerator AdvProcess()
    {
        print("ADV START");
        PauseManager.PauseWithoutInterface(true);

        float timer = 0f;
        float timerSend = 2f;

        while (PauseManager.instance.isPause)
        {
            timer += Time.unscaledDeltaTime; // Используем Time.unscaledDeltaTime, чтобы игнорировать Time.timeScale
            timerSend -= Time.unscaledDeltaTime;

            switch (YG2.lang)
            {
                case "ru":
                    advText.text = "Реклама будет показана через " + "<color=yellow>" + timerSend.ToString("F1") + "</color>" + " секунды";
                    break;
                case "en":
                    advText.text = "The ad will be displayed in " + "<color=yellow>" + timerSend.ToString("F1") + "</color>" + " seconds";
                    break;
                case "tr":
                    advText.text = "Reklam " + "<color=yellow>" + timerSend.ToString("F1") + "</color>" + " saniye sonra gosterilecek";
                    break;
            }

            if (timer >= 2f)
            {
                YG2.InterstitialAdvShow();

                // Показать уведомление что игрок получил монетку
                _advPanel.gameObject.SetActive(false);
                PauseManager.instance.PauseAd(true);

                break; // Выход из цикла, если условие выполнилось
            }

            yield return null;
        }
    }

}
