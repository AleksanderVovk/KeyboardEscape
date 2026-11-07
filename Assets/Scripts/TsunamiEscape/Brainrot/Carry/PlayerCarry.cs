using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class PlayerCarry : MonoBehaviour
{
    // Текущие брейнроты в руках
    public List<BrainrotInWorld> brainrotsInHand = new();

    [Header("Hand points (slots in hand)")]
    [SerializeField] private Transform[] _handPoints;   // точки, куда крепим брейнроты по очереди

    [Header("UI")]
    [SerializeField] private Button throwButton;         // кнопка "кинуть" в UI

    private Player owner;

    #region Init

    public void Initialization(Player owner)
    {
        this.owner = owner;

        brainrotsInHand.Clear();

        if (owner.thisRealPlayer && throwButton != null)
        {
            throwButton.onClick.RemoveAllListeners();
            throwButton.onClick.AddListener(OnThrowPressed);
            throwButton.gameObject.SetActive(false);
        }
    }

    #endregion


    #region Pick up

    /// <summary>
    /// Поднять брейнрот, если есть свободный слот по апгрейду и свободная точка в руках,
    /// а также хватает места на базе.
    /// </summary>
    public void SetCarryObject(BrainrotInWorld brainrotInWorld)
    {
        if (brainrotInWorld == null)
            return;

        // Максимум слотов по апгрейду
        int maxCarryByUpgrade = YG2.saves.playerData.shop.carrySlots + 1;
        if (maxCarryByUpgrade <= 0)
            maxCarryByUpgrade = 1;

        // Проверка лимита "в руках" по апгрейду
        if (brainrotsInHand.Count >= maxCarryByUpgrade)
        {
            if (owner.thisRealPlayer)
            {
                NotificationManager.ShowNotification(
                    UTF.ReadLanguageDirectory(LangList.instance.handAreBusy),
                    NotificationType.Error
                );
            }
            return;
        }

        // 1) Проверяем, что у базы вообще есть ссылка
        var baseRef = owner != null ? owner.tsunamiReference?.playerBase : null;
        if (baseRef == null)
            return;

        // 2) Считаем, сколько всего СВОБОДНЫХ поинтов на базе
        int freePoints = 0;
        if (baseRef.brainrotPoints != null)
        {
            for (int i = 0; i < baseRef.brainrotPoints.Length; i++)
            {
                var p = baseRef.brainrotPoints[i];
                if (p == null) continue;

                // поинт должен быть открыт (activeInHierarchy) и пуст
                if (!p.gameObject.activeInHierarchy) continue;
                if (p.brainrotInWorld == null)
                    freePoints++;
            }
        }

        // 3) Уже несём N брейнротов, пытаемся взять ещё один
        int alreadyCarried = brainrotsInHand.Count;

        // Если свободных поинтов меньше, чем (уже несём + этот новый),
        // то на базе всех не разместим — не берём.
        if (freePoints < alreadyCarried + 1)
        {
            if (owner.thisRealPlayer)
            {
                NotificationManager.ShowNotification(
                    UTF.ReadLanguageDirectory(LangList.instance.noFreeBrainrotPoint),
                    NotificationType.Error
                );
            }
            return;
        }

        // Проверка: есть ли свободная точка в руках
        int freeIndex = FindFreeHandPointIndex();
        if (freeIndex == -1)
        {
            if (owner.thisRealPlayer)
            {
                NotificationManager.ShowNotification(
                    UTF.ReadLanguageDirectory(LangList.instance.handAreBusy),
                    NotificationType.Error
                );
            }
            return;
        }

        // Берём предмет
        brainrotsInHand.Add(brainrotInWorld);

        brainrotInWorld.ChangeBrainrotState(BrainrotState.InHand);
        brainrotInWorld.transform.SetParent(_handPoints[freeIndex], false);
        brainrotInWorld.transform.localPosition = Vector3.zero;
        brainrotInWorld.transform.localRotation = _handPoints[freeIndex].localRotation;

        SetCarryState(brainrotsInHand.Count > 0);

        UISound.manager.PlayPop(true);
        YG2.MetricaSend("pickup_brainrot");
    }


    public void DestroyBrainrotInHand(BrainrotInWorld target)
    {
        if (target == null) return;

        brainrotsInHand.Remove(target);
        Destroy(target.gameObject);
        SetCarryState(brainrotsInHand.Count > 0);
    }


    /// <summary>
    /// Ищем индекс первой свободной точки в руках.
    /// Свободна, если нет ребёнка с BrainrotInWorld.
    /// </summary>
    private int FindFreeHandPointIndex()
    {
        if (_handPoints == null || _handPoints.Length == 0)
            return -1;

        for (int i = 0; i < _handPoints.Length; i++)
        {
            Transform point = _handPoints[i];
            if (point == null)
                continue;

            bool hasBrainrot = false;
            for (int c = 0; c < point.childCount; c++)
            {
                if (point.GetChild(c).GetComponent<BrainrotInWorld>() != null)
                {
                    hasBrainrot = true;
                    break;
                }
            }

            if (!hasBrainrot)
                return i;
        }

        return -1; // нет свободной точки
    }

    #endregion


    #region Carry state

    public void SetCarryState(bool isCarry)
    {
        if (owner != null && owner.animator != null)
            owner.animator.SetBool("isCarry", isCarry);

        if (owner != null && owner.thisRealPlayer && throwButton != null)
            throwButton.gameObject.SetActive(isCarry);
    }

    /// <summary>
    /// Полностью уничтожить все брейнроты в руках (если нужно).
    /// </summary>
    public void DestroyAllBrainrotsInHand()
    {
        for (int i = 0; i < brainrotsInHand.Count; i++)
        {
            if (brainrotsInHand[i] != null)
                Destroy(brainrotsInHand[i].gameObject);
        }

        brainrotsInHand.Clear();
        SetCarryState(false);
    }

    #endregion


    #region Throw logic

    /// <summary>
    /// Вызывается из UI‑кнопки "кинуть".
    /// Сейчас бросаем один последний взятый брейнрот.
    /// (Можно изменить стратегию: первый, рандом, все сразу и т.п.)
    /// </summary>
    public void OnThrowPressed()
    {
        if (brainrotsInHand.Count == 0)
            return;

        if (owner != null && !owner.thisRealPlayer)
        {
            // NPC — выбрасывает всех сразу
            ThrowAll();
        }
        else
        {
            // Игрок — как и было, только одного (последнего)
            BrainrotInWorld toThrow = brainrotsInHand[brainrotsInHand.Count - 1];
            ThrowAll();
        }
    }


    /// <summary>
    /// Бросить один конкретный брейнрот из рук.
    /// </summary>
    private void ThrowOne(BrainrotInWorld toThrow)
    {
        if (toThrow == null)
            return;

        if (!brainrotsInHand.Contains(toThrow))
            return;

        brainrotsInHand.Remove(toThrow);

        Vector3 forward = owner.transform.forward;

        // Запоминаем точку, с которой он бросается
        Transform fromPoint = toThrow.transform.parent;
        toThrow.transform.SetParent(null);

        bool stillCarry = brainrotsInHand.Count > 0;
        SetCarryState(stillCarry);

        // Если по какой‑то причине fromPoint null — берём первую точку
        Transform startPoint = fromPoint != null && fromPoint is Transform
            ? fromPoint
            : (_handPoints != null && _handPoints.Length > 0 ? _handPoints[0] : owner.transform);

        toThrow.ThrowFrom(startPoint, forward);
    }

    /// <summary>
    /// Если вдруг захочешь бросать все сразу — можно вызывать это вместо ThrowOne/OnThrowPressed.
    /// </summary>
    public void ThrowAll()
    {
        if (brainrotsInHand.Count == 0)
            return;

        Vector3 forward = owner.transform.forward;

        for (int i = 0; i < brainrotsInHand.Count; i++)
        {
            BrainrotInWorld br = brainrotsInHand[i];
            if (br == null)
                continue;

            Transform fromPoint = br.transform.parent;
            br.transform.SetParent(null);

            Transform startPoint = fromPoint != null && fromPoint is Transform
                ? fromPoint
                : (_handPoints != null && _handPoints.Length > 0 ? _handPoints[0] : owner.transform);

            br.ThrowFrom(startPoint, forward);
        }

        brainrotsInHand.Clear();
        SetCarryState(false);
    }

    #endregion
}
