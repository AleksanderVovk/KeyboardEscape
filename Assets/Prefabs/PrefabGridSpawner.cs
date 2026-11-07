using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabGridSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject _prefab;

    [Header("Grid Size")]
    [Min(1)][SerializeField] private int _countX = 3;
    [Min(1)][SerializeField] private int _countZ = 3;

    [Header("Spacing")]
    [SerializeField] private float _offsetX = 0f;
    [SerializeField] private float _offsetZ = 0f;

    [Header("Options")]
    [SerializeField] private bool _clearPrevious = true;
    [SerializeField] private string _rootName = "SpawnedGrid";

    [ContextMenu("Spawn Grid")]
    public void SpawnGrid()
    {
        if (_prefab == null)
        {
            Debug.LogError("Prefab is null.");
            return;
        }

        BoxCollider box = _prefab.GetComponent<BoxCollider>();
        if (box == null)
        {
            Debug.LogError("Prefab root must have BoxCollider.");
            return;
        }

        if (_clearPrevious)
            ClearPrevious();

        GameObject root = new GameObject(_rootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Vector3 prefabScale = _prefab.transform.localScale;
        Vector3 boxSize = Vector3.Scale(box.size, prefabScale);
        Vector3 boxCenter = Vector3.Scale(box.center, prefabScale);

        float stepX = boxSize.x + _offsetX;
        float stepZ = boxSize.z + _offsetZ;

        Vector3 start = new Vector3(
            -(((_countX - 1) * stepX) * 0.5f),
            0f,
            -(((_countZ - 1) * stepZ) * 0.5f)
        );

        for (int z = 0; z < _countZ; z++)
        {
            for (int x = 0; x < _countX; x++)
            {
                GameObject go = Instantiate(_prefab, root.transform);
                go.name = $"{_prefab.name}_{x}_{z}";
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = prefabScale;

                Vector3 pos = start + new Vector3(x * stepX, 0f, z * stepZ);
                go.transform.localPosition = pos - boxCenter;
            }
        }
    }

    [ContextMenu("Clear Previous")]
    public void ClearPrevious()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}