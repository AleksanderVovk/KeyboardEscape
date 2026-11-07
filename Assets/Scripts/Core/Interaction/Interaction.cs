using System.Collections;
using UnityEngine;

public class Interaction : MonoBehaviour
{
    public bool canInteraction = true;
    private Player _playerInTrigger;

    [Space(15f)]
    [Header("Settings")]
    public bool turnOffAfterUse;

    [Space(15f)]
    public float holdSpeed = 2f;

    [Space(15f)]
    public LanguageDirectory interactionName;
    public LanguageDirectory interactionDescription;
    public Vector3 offset = new Vector3(0f, 1.25f, 0f);

    private Coroutine _checkRoutine;

    public virtual void Action(Player player)
    {
        if (turnOffAfterUse)
            canInteraction = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null)
            return;

        _playerInTrigger = player;

        if (player.thisRealPlayer)
        {
            // Если тебе больше не нужна периодическая проверка — можно удалить корутину полностью.
            if (_checkRoutine == null)
            {
                _checkRoutine = StartCoroutine(CheckPlayerRoutine());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player != _playerInTrigger)
            return;

        _playerInTrigger = null;

        if (_checkRoutine != null)
        {
            StopCoroutine(_checkRoutine);
            _checkRoutine = null;
        }
    }

    // теперь корутина вообще не обязательна, можешь удалить её,
    // если не делаешь внутри дополнительных проверок.
    private IEnumerator CheckPlayerRoutine()
    {
        while (_playerInTrigger != null)
        {
            yield return new WaitForSeconds(0.25f);

            if (_playerInTrigger != null && _playerInTrigger.thisRealPlayer)
            {
                // дополнительные проверки логики, если нужны
            }
        }

        _checkRoutine = null;
    }
}
