using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SetOtuline : MonoBehaviour
{
    public float otulineWidth = 0.106f;
    public Color color;

    private void Awake()
    {
        TMP_Text textmeshPro = GetComponent<TMP_Text>();
        textmeshPro.outlineWidth = otulineWidth;
        textmeshPro.outlineColor = color;
    }
}
