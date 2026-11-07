using TMPro;
using UnityEngine;

public class WaveInstance : MonoBehaviour
{
    [SerializeField] private float _speed;
    [SerializeField] private Vector3 _dir = Vector3.forward;
    [SerializeField] private float _lifeTime = 60f;

    [SerializeField] private TMP_Text _waveNameDisplay;
    [SerializeField] private SpriteRenderer _waveIconDisplay;

    private WaveDefinitionSO _waveData;
    private Vector3 _endPoint;
    private float _destroyDistanceSqr = 4f; // 2м в квадрате для оптимизации

    public WaveDefinitionSO WaveData => _waveData;

    public void Init(WaveDefinitionSO waveData, float speed, Vector3 direction, Vector3 endPoint, float lifeTime)
    {
        _waveData = waveData;
        _speed = speed;
        _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        _endPoint = endPoint;
        _lifeTime = Mathf.Max(0.1f, lifeTime);

        if (_waveData == null)
        {
            Destroy(gameObject);
            return;
        }

        if (_waveNameDisplay != null)
            _waveNameDisplay.text = UTF.ReadLanguageDirectory(_waveData.waveName);

        if (_waveIconDisplay != null)
            _waveIconDisplay.sprite = _waveData.waveIcon;

        Destroy(gameObject, _lifeTime);
    }

    private void Update()
    {
        transform.position += _dir * (_speed * Time.deltaTime);

        // Проверяем, достигли ли конечной точки
        float distSqr = (transform.position - _endPoint).sqrMagnitude;
        if (distSqr <= _destroyDistanceSqr)
        {
            Destroy(gameObject);
        }
    }
}
