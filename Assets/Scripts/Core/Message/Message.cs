using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Message : MonoBehaviour
{
    public static Message instance;

    public TMP_Text messageText;

    public float speed = 1.0f;

    private Vector3 startPosition;
    private Color startColor = Color.white;
    private float alpha = 1.0f;
    public bool isAnimating = false;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        startPosition = transform.position;
        startColor = messageText.color;
        messageText.text = "";
    }

    public static void ShowMessage(string message) => instance._ShowMessage(message);
    public void _ShowMessage(string message)
    {
        StopAllCoroutines();
        messageText.gameObject.SetActive(true);
        transform.position = startPosition;
        messageText.color = startColor;
        alpha = 1.0f;

        UISound.manager.PlayError(false);

        messageText.text = message;
        StartCoroutine(FadeMessage());
    }

    IEnumerator FadeMessage()
    {
        isAnimating = true;

        while (alpha > 0.0f)
        {
            alpha -= Time.deltaTime * speed;
            messageText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        messageText.text = "";
        transform.position = startPosition;
        messageText.color = startColor;
        alpha = 1.0f;
        messageText.gameObject.SetActive(false);
        isAnimating = false;
    }
}
