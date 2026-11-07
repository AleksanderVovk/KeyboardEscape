using UnityEngine;
using DG.Tweening;

public class InappShake : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private RectTransform target;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeStrength = 18f;
    [SerializeField] private int vibrato = 25;
    [SerializeField, Range(0f, 180f)] private float randomness = 90f;

    [Header("Rotate (always)")]
    [SerializeField] private float rotateHalfPeriod = 0.35f; // время до смены стороны (влево->вправо)
    [SerializeField] private float rotateAngle = 8f;

    [Header("Pause")]
    [SerializeField] private float pauseSeconds = 2f;

    private Sequence shakeSeq;
    private Tween rotateTween;

    private Vector2 baseAnchoredPos;
    private Vector3 baseLocalEuler;

    private void Awake()
    {
        if (target == null) target = (RectTransform)transform;

        baseAnchoredPos = target.anchoredPosition;
        baseLocalEuler = target.localEulerAngles;
    }

    private void OnEnable() => Play();
    private void OnDisable() => Stop();

    [ContextMenu("Play")]
    public void Play()
    {
        Stop();

        target.anchoredPosition = baseAnchoredPos;
        target.localEulerAngles = baseLocalEuler;

        // Вращение всегда: туда-сюда бесконечно через SetLoops(-1, Yoyo) [web:18]
        rotateTween = target
            .DORotate(new Vector3(baseLocalEuler.x, baseLocalEuler.y, baseLocalEuler.z + rotateAngle), rotateHalfPeriod)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        // Шейк + пауза в цикле отдельно (anchoredPosition для UI) [web:11]
        shakeSeq = DOTween.Sequence()
            .Append(
                target.DOShakeAnchorPos(
                    shakeDuration,
                    strength: new Vector2(shakeStrength, shakeStrength * 0.55f),
                    vibrato: vibrato,
                    randomness: randomness,
                    snapping: false,
                    fadeOut: true
                )
            )
            .AppendInterval(pauseSeconds)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true);
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (shakeSeq != null) shakeSeq.Kill();
        shakeSeq = null;

        if (rotateTween != null) rotateTween.Kill();
        rotateTween = null;

        if (target != null)
        {
            target.anchoredPosition = baseAnchoredPos;
            target.localEulerAngles = baseLocalEuler;
        }
    }
}
