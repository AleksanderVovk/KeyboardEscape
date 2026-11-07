using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageView : MonoBehaviour
{
    private TMP_Text _textView;
    private Animator _animator;

    private void Awake()
    {
        _textView = GetComponent<TMP_Text>();
        _animator = GetComponent<Animator>();
    }

    public void Show(float time)
    {
        if (time > 0)
            StartCoroutine(ShowTimer(time));
        else
            StartShow();
    }

    public void SetMessage(string message)
    {
        _textView.text = message;
    }

    private IEnumerator ShowTimer(float time)
    {
        yield return new WaitForSeconds(time);

        _animator.SetTrigger("Hide");
    }

    public void StartShow()
    {
        _animator.SetTrigger("Show");

        _textView.enabled = true;
    }

    public void StartHide()
    {
        _animator.SetTrigger("Hide");
    }

    public void Hide()
    {
        _textView.enabled = false;
    }
}
