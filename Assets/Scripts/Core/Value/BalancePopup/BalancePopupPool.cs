using UnityEngine;

public class BalancePopupPool : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private RectTransform parent;      // куда спавнить (например, та же панель где баланс)
    [SerializeField] private BalancePopupView popupPrefab;
    [SerializeField] private RectTransform spawnPoint;  // ОДНА точка (anchoredPosition)
    [SerializeField] private int poolSize = 3;

    [Header("Anim")]
    [SerializeField] private float inTime = 0.10f;
    [SerializeField] private float outTime = 0.45f;
    [SerializeField] private float floatUp = 26f;

    private BalancePopupView[] _pool;
    private int _cursor;

    private void Awake()
    {
        poolSize = Mathf.Clamp(poolSize, 1, 3);

        _pool = new BalancePopupView[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var v = Instantiate(popupPrefab, spawnPoint); // parent = spawnPoint
            v.gameObject.SetActive(false);
            _pool[i] = v;
        }
    }

    public void Show(long delta)
    {
        var v = _pool[_cursor];
        _cursor = (_cursor + 1) % _pool.Length;

        v.Play(delta, inTime, outTime, floatUp);
    }
}
