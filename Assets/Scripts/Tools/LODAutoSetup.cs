using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class LODAutoSetup : MonoBehaviour
{
    public bool autoAddAllRenderers = true;
    public bool clearExistingLOD = true;
    [Range(0f, 1f)]
    public float lod0ScreenRelativeTransitionHeight = 1f;

    private LODGroup lodGroup;

    void Reset()
    {
        SetupLOD();
    }

    [ContextMenu("Setup LOD")]
    public void SetupLOD()
    {
        lodGroup = GetComponent<LODGroup>();
        if (lodGroup == null)
        {
            lodGroup = gameObject.AddComponent<LODGroup>();
        }

        if (clearExistingLOD)
        {
            LOD[] emptyLODs = new LOD[0];
            lodGroup.SetLODs(emptyLODs);
        }

        if (autoAddAllRenderers)
        {
            AddAllChildRenderers();
        }
    }

    [ContextMenu("Add All Child Renderers")]
    public void AddAllChildRenderers()
    {
        if (lodGroup == null)
        {
            lodGroup = GetComponent<LODGroup>();
            if (lodGroup == null) return;
        }

        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);

        List<Renderer> validRenderers = new List<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            if (renderer != null &&
                (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) &&
                renderer.gameObject != gameObject)
            {
                validRenderers.Add(renderer);
            }
        }

        if (validRenderers.Count > 0)
        {
            LOD[] lods = new LOD[1];

            LOD lod0 = new LOD();
            lod0.screenRelativeTransitionHeight = lod0ScreenRelativeTransitionHeight;
            lod0.renderers = validRenderers.ToArray();

            lods[0] = lod0;

            lodGroup.SetLODs(lods);
        }
    }
}