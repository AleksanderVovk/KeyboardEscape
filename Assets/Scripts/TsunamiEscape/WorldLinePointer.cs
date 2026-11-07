using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WorldLinePointer : MonoBehaviour
{
    [Header("Refs")]
    public Transform from;
    public Transform target;

    [Header("Offsets")]
    public Vector3 fromOffset = new Vector3(0, 0.1f, 0);
    public Vector3 targetOffset = new Vector3(0, 0.1f, 0);

    [Header("Visual")]
    public float startWidth = 0.08f;
    public float endWidth = 0.08f;
    public int sortingOrder = 10;

    private LineRenderer _lr;
    private bool _forcedVisible;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true; // world space [web:109]
    }

    public void SetFrom(Transform t) => from = t;
    public void SetTarget(Transform t) => target = t;

    public void Show(bool show)
    {
        _forcedVisible = show;
        _lr.enabled = show; // не завязываемся на target
    }

    private void LateUpdate()
    {
        if (!_forcedVisible || from == null || target == null)
        {
            _lr.enabled = false;
            return;
        }

        _lr.enabled = true;
        _lr.SetPosition(0, from.position + fromOffset);
        _lr.SetPosition(1, target.position + targetOffset);
    }
}
