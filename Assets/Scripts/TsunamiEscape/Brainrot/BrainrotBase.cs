using UnityEngine;

public abstract class BrainrotBase : ScriptableObject
{
    public string id;

    public Sprite icon;
    public LanguageDirectory brainrotName;
    public LanguageDirectory brainrotDescription;

    public BrainrotRare brainrotRare;
    public GameObject brainrotPrefab; // всегда BrainrotInWorld

    public int earningPerSecond; // для обычных, у лаки-блока может быть 0

    // флаг для удобной проверки типа
    public abstract bool IsLucky { get; }
}
