using UnityEngine;

[CreateAssetMenu(fileName = "New Skin", menuName = "GameData/Customization/Skin", order = 1)]
public class Skin : ScriptableObject
{
    public string id;

    public LanguageDirectory skinName;

    public Material skinBodyMaterial;
    public Material skinFaceMaterial;

    public Sprite icon;
    public Price price;
}