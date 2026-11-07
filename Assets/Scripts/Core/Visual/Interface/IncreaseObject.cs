using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IncreaseObject : MonoBehaviour
{
    [SerializeField] private Vector2 _newSize;

    public bool thisEnterCursor;
    private Image _image;
    private Vector2 _defaultSize;

    private void Start()
    {
        _image = GetComponent<Image>();
        _defaultSize = _image.rectTransform.sizeDelta;
    }

    public void SetEnterCursor(bool value)
    {
        thisEnterCursor = value;

        //if (thisEnterCursor)
            //SoundPlay.manager.Play_Hover(true);
    }

    private void Update()
    {
        if (thisEnterCursor)
        {
            _image.rectTransform.sizeDelta = Vector2.Lerp(_image.rectTransform.sizeDelta, new Vector2(_newSize.x, _newSize.y), Time.deltaTime * 10f);
        }
        else
        {
            if (_image.rectTransform.sizeDelta != _defaultSize)
                _image.rectTransform.sizeDelta = Vector2.Lerp(_image.rectTransform.sizeDelta, _defaultSize, Time.deltaTime * 10f);
        }
    }
}
