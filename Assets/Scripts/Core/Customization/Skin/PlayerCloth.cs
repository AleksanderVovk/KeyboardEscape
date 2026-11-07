using System.Collections.Generic;
using UnityEngine;
using YG;

public class PlayerCloth : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] bodyRenderers;
    [SerializeField] private Renderer[] faceRenderers;

    [SerializeField] private ParticleSystem _equipParticle;

    [Header("Bot Settings")]
    [SerializeField] private bool isBot = false;

    private Skin botSkin = null;
    private bool isInitialized = false;

    // Храним созданные материалы для корректного удаления
    private Material currentBodyMat;
    private Material currentFaceMat;

    public void Initialization()
    {
        if (!isBot)
        {
            SkinManager skinManager = FindFirstObjectByType<SkinManager>();
            if (skinManager != null)
            {
                Skin equippedSkin = SkinManager.instance.GetSkinById(YG2.saves.playerData.equippedSkinId);
                if (equippedSkin != null)
                    EquipSkin(equippedSkin, false);
            }
        }
    }

    private void OnDestroy()
    {
        // Очищаем созданные материалы
        if (currentBodyMat != null) Destroy(currentBodyMat);
        if (currentFaceMat != null) Destroy(currentFaceMat);
    }

    public void ApplyRandomBotSkin()
    {
        if (isInitialized)
            return;

        SkinManager skinManager = FindFirstObjectByType<SkinManager>();
        if (skinManager == null)
            return;

        List<Skin> allSkins = GetAllAvailableSkins(skinManager);

        if (allSkins == null || allSkins.Count == 0)
        {
            Debug.LogWarning("No skins available for bot!");
            return;
        }

        Skin randomSkin = allSkins[Random.Range(0, allSkins.Count)];

        if (randomSkin != null)
        {
            botSkin = randomSkin;
            EquipSkin(randomSkin, false);
            isInitialized = true;
            Debug.Log($"Bot equipped random skin: {randomSkin.name}");
        }
    }

    private List<Skin> GetAllAvailableSkins(SkinManager skinManager)
    {
        List<Skin> skins = new List<Skin>();

        var fieldInfo = typeof(SkinManager).GetField("allSkins",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            var allSkins = fieldInfo.GetValue(skinManager) as List<Skin>;
            if (allSkins != null)
            {
                skins.AddRange(allSkins);
            }
        }

        return skins;
    }

    public void EquipSkin(Skin skin, bool needPlayParticle)
    {
        if (skin == null)
            return;

        Debug.Log($"Equip skin {(isBot ? "for bot" : "for real player")}: {skin.name}");

        // Удаляем старые материалы
        if (currentBodyMat != null) Destroy(currentBodyMat);
        if (currentFaceMat != null) Destroy(currentFaceMat);

        // Создаем КОПИИ материалов для этого конкретного объекта
        currentBodyMat = new Material(skin.skinBodyMaterial);
        currentFaceMat = new Material(skin.skinFaceMaterial);

        // Применяем уникальные материалы
        ApplyMaterial(bodyRenderers, currentBodyMat);
        ApplyMaterial(faceRenderers, currentFaceMat);

        if (!isBot && needPlayParticle)
            _equipParticle?.Play();
    }

    private void ApplyMaterial(Renderer[] renderers, Material material)
    {
        if (renderers == null || renderers.Length == 0 || material == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                // Используем material (создает копию) вместо sharedMaterial
                renderers[i].material = material;
            }
        }
    }
}