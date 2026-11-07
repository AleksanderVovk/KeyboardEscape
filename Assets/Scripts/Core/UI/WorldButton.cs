using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WorldButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    [Header("Fake click")]
    [SerializeField] private float pressTime = 0.08f;

    private Coroutine _routine;

    public void OnWorldClick()
    {
        if (_button == null) return;
        if (!EnsureEventSystem()) return;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PressRoutine());
    }

    private IEnumerator PressRoutine()
    {
        var go = _button.gameObject;

        // Создаём PointerEventData и "нажимаем"
        var data = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(go, data, ExecuteEvents.pointerDownHandler); // визуально Pressed [web:208]

        yield return new WaitForSeconds(pressTime);

        // "Отпускаем" и "кликаем" (чтобы сработал OnClick)
        ExecuteEvents.Execute(go, data, ExecuteEvents.pointerUpHandler);   // вернёт состояние обратно [web:214]
        ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);

        _routine = null;
    }

    private bool EnsureEventSystem()
    {
        if (EventSystem.current != null) return true;
        Debug.LogWarning("WorldButton: No EventSystem in scene (UI transitions won't work).");
        return false;
    }
}
