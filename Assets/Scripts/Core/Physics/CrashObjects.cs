using System.Collections;
using UnityEngine;

public class CrashObjects : MonoBehaviour
{
    [SerializeField] private GameObject[] _animationObjects;
    [SerializeField] private float _timeBetweenObjects = 0.2f;
    [SerializeField] private float _parabolaDuration = 1f;
    [SerializeField] private float _parabolaHeight = 2f;
    [SerializeField] private float _parabolaHorizontalDistance = 2f;

    [SerializeField] private GameObject _startSpawnObject;
    public void StartCrash()
    {
        StartCoroutine(AnimateObjectsCoroutine());
    }

    public void ForceHideObjects()
    {
        for (int i = 0; i < _animationObjects.Length; i++)
        {
            _animationObjects[i].SetActive(false);
        }
    }

    private IEnumerator AnimateObjectsCoroutine()
    {
        foreach (var obj in _animationObjects)
        {
            if (obj == null) continue;

            foreach (var col in obj.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            Vector3 randomDir = new Vector3(
                Random.Range(-_parabolaHorizontalDistance, _parabolaHorizontalDistance),
                0f,
                Random.Range(-_parabolaHorizontalDistance, _parabolaHorizontalDistance)
            );

            StartCoroutine(ParabolaMove(obj, obj.transform.position, obj.transform.position + randomDir, _parabolaHeight, _parabolaDuration));

            yield return new WaitForSeconds(_timeBetweenObjects);
        }
    }

    private IEnumerator ParabolaMove(GameObject obj, Vector3 startPos, Vector3 endPos, float height, float duration)
    {
        float elapsed = 0f;

        Destroy(Instantiate(_startSpawnObject, obj.transform.position, Quaternion.identity), 5f);
        UISound.manager.PlayPop(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            pos.y += Mathf.Sin(t * Mathf.PI) * height;

            obj.transform.position = pos;

            yield return null;
        }

        Destroy(obj);

        OnObjectRemoved(obj);
    }

    private void OnObjectRemoved(GameObject obj)
    {
        Debug.Log($"Object {obj.name} removed!");
    }
}
