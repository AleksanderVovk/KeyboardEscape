using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class MoneyRewardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _value;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetData(int value)
    {
        _value.gameObject.SetActive(true);

        _animator.SetTrigger("Show");
        _value.text = "<color=white>+</color>" + "<color=green>" + value + "</color>" + UTF.manager.TextToSprite("\U0001F601");

        StopAllCoroutines();
        StartCoroutine(Hide());
    }

    public IEnumerator Hide()
    {
        yield return new WaitForSeconds(1.5f);

        _animator.SetTrigger("Hide");

        yield return null;
    }

    public void HideText()
    {
        _value.gameObject.SetActive(false);
    }
}
