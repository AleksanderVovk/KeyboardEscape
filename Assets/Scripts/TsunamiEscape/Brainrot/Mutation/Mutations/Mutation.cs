using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "Mutation", menuName = "GameData/Plays/TsunamiEscape/Mutation", order = 1)]
public class Mutation : ScriptableObject
{
    public string id;

    public LanguageDirectory mutationName;
    public LanguageDirectory mutationDescription;

    public float multiplier = 1f;

    [Range(0f, 1f)] public float chance = 0.05f;
    public bool isEvent = false;

    [Header("Visuals")]
    public Material overrideMaterial;
    public GameObject centerEffectPrefab;

    [Header("UI Color")]
    public bool useGradient = false;
    public TMP_ColorGradient gradientPreset; // если useGradient=true
    public Color color = Color.white;        // если градиента нет
}
