using System.Collections.Generic;
using UnityEngine;

public class KeyboardInteractionManager : MonoBehaviour
{
    [Header("Keyboard Settings")]
    [SerializeField] private float keyPressDepth = 0.03f;
    [SerializeField] private float pressDownSpeed = 10f;
    [SerializeField] private float pressUpSpeed = 8f;
    [SerializeField] private float releaseDelayAfterFullPress = 0.01f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] keySounds;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float soundCooldown = 0.1f;

    [Header("Dynamic Registration")]
    [SerializeField] private bool autoRegisterNewKeys = true;
    [SerializeField] private int maxRegisteredKeys = 100;

    private readonly Dictionary<Collider, KeyData> keyDictionary = new Dictionary<Collider, KeyData>();
    private readonly HashSet<Collider> currentTriggers = new HashSet<Collider>();
    private readonly List<Collider> registrationOrder = new List<Collider>();

    private float lastSoundTime;

    private enum KeyState
    {
        Idle,
        Pressing,
        Held,
        WaitingRelease,
        Releasing
    }

    [System.Serializable]
    private class KeyData
    {
        public Transform visual;
        public Vector3 originalWorldPos;
        public float pressProgress;
        public KeyState state;
        public float stateTimer;
        public bool isInsideTrigger;
        public float lastRegisteredTime;
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EnsureKeyRegisteredWithEviction(other))
            return;

        currentTriggers.Add(other);

        KeyData data = keyDictionary[other];
        data.isInsideTrigger = true;

        if (data.state == KeyState.Idle || data.state == KeyState.Releasing || data.state == KeyState.WaitingRelease)
            data.state = KeyState.Pressing;

        if (Time.time - lastSoundTime >= soundCooldown)
        {
            PlayRandomKeySound();
            lastSoundTime = Time.time;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!keyDictionary.TryGetValue(other, out KeyData data))
            return;

        currentTriggers.Add(other);
        data.isInsideTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!keyDictionary.TryGetValue(other, out KeyData data))
            return;

        currentTriggers.Remove(other);
        data.isInsideTrigger = false;
    }

    private void Update()
    {
        foreach (var pair in keyDictionary)
        {
            Collider col = pair.Key;
            KeyData data = pair.Value;

            bool inside = currentTriggers.Contains(col);
            data.isInsideTrigger = inside;

            switch (data.state)
            {
                case KeyState.Idle:
                    UpdateIdle(data, inside);
                    break;
                case KeyState.Pressing:
                    UpdatePressing(data, inside);
                    break;
                case KeyState.Held:
                    UpdateHeld(data, inside);
                    break;
                case KeyState.WaitingRelease:
                    UpdateWaitingRelease(data, inside);
                    break;
                case KeyState.Releasing:
                    UpdateReleasing(data, inside);
                    break;
            }

            ApplyVisualPosition(data);
        }
    }

    private void UpdateIdle(KeyData data, bool inside)
    {
        data.pressProgress = Mathf.MoveTowards(data.pressProgress, 0f, pressUpSpeed * Time.deltaTime);

        if (inside)
            data.state = KeyState.Pressing;
    }

    private void UpdatePressing(KeyData data, bool inside)
    {
        data.pressProgress = Mathf.MoveTowards(data.pressProgress, 1f, pressDownSpeed * Time.deltaTime);

        if (data.pressProgress >= 1f - 0.0001f)
        {
            data.pressProgress = 1f;
            data.stateTimer = 0f;

            if (inside)
            {
                data.state = KeyState.Held;
            }
            else
            {
                data.state = KeyState.WaitingRelease;
            }
        }
    }

    private void UpdateHeld(KeyData data, bool inside)
    {
        if (!inside)
        {
            data.state = KeyState.WaitingRelease;
            data.stateTimer = 0f;
        }
        else
        {
            data.pressProgress = 1f;
        }
    }

    private void UpdateWaitingRelease(KeyData data, bool inside)
    {
        // если игрок вернулся — снова держим кнопку
        if (inside)
        {
            data.state = KeyState.Held;
            data.pressProgress = 1f;
            return;
        }

        data.stateTimer += Time.deltaTime;
        if (data.stateTimer >= releaseDelayAfterFullPress)
            data.state = KeyState.Releasing;
    }

    private void UpdateReleasing(KeyData data, bool inside)
    {
        // если игрок опять наступил — обратно в Pressing
        if (inside)
        {
            data.state = KeyState.Pressing;
            return;
        }

        data.pressProgress = Mathf.MoveTowards(data.pressProgress, 0f, pressUpSpeed * Time.deltaTime);

        if (data.pressProgress <= 0f + 0.0001f)
        {
            data.pressProgress = 0f;
            data.state = KeyState.Idle;
            data.stateTimer = 0f;
        }
    }

    private void ApplyVisualPosition(KeyData data)
    {
        if (data.visual == null)
            return;

        Vector3 pressDirection = CalculatePressDirectionFromFace(data.visual);
        data.visual.position = data.originalWorldPos + pressDirection * (data.pressProgress * keyPressDepth);
    }

    private Vector3 CalculatePressDirectionFromFace(Transform keyVisual)
    {
        Vector3 projectedDown = Vector3.ProjectOnPlane(Vector3.down, keyVisual.forward);
        return projectedDown.sqrMagnitude > 0.0001f ? projectedDown.normalized : -keyVisual.up;
    }

    /// <summary>Регистрация с вытеснением старых при переполнении.</summary>
    private bool EnsureKeyRegisteredWithEviction(Collider other)
    {
        if (keyDictionary.ContainsKey(other))
            return true;

        if (!autoRegisterNewKeys || !other.CompareTag("KeyboardKey"))
            return false;

        if (keyDictionary.Count >= maxRegisteredKeys)
        {
            EvictOldestKey();
        }

        RegisterKeyData(other, other.transform);
        return keyDictionary.ContainsKey(other);
    }

    /// <summary>Вытеснение: старая кнопка дожимается до 1, ждёт 0.01 и только потом поднимается.</summary>
    private void EvictOldestKey()
    {
        if (registrationOrder.Count == 0)
            return;

        Collider oldest = registrationOrder[0];
        registrationOrder.RemoveAt(0);

        if (!keyDictionary.TryGetValue(oldest, out KeyData data))
            return;

        // Не меняем currentTriggers и data.isInsideTrigger — пусть они отражают реальность.
        // Просто гарантируем, что клавиша не будет «новой» и со временем отпустится.

        // Если она ещё не полностью нажата — можно сразу довести до 1, чтобы не «залипала»
        if (data.pressProgress < 1f - 0.0001f)
        {
            data.pressProgress = 1f;
        }

        // Ставим в WaitingRelease, но реальный отпуск произойдёт только если !inside в Update
        if (data.state == KeyState.Idle || data.state == KeyState.Pressing || data.state == KeyState.Held)
        {
            data.state = KeyState.WaitingRelease;
            data.stateTimer = 0f;
        }
    }

    private void RegisterKeyData(Collider collider, Transform keyTransform)
    {
        if (keyDictionary.ContainsKey(collider))
            return;

        Transform visual = FindKeyVisual(keyTransform);
        if (visual == null)
            return;

        var data = new KeyData
        {
            visual = visual,
            originalWorldPos = visual.position,
            pressProgress = 0f,
            state = KeyState.Idle,
            stateTimer = 0f,
            isInsideTrigger = false,
            lastRegisteredTime = Time.time
        };

        keyDictionary.Add(collider, data);
        registrationOrder.Add(collider);

        if (!collider.enabled)
            collider.enabled = true;

        Rigidbody rb = keyTransform.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
            rb.isKinematic = true;
    }

    private Transform FindKeyVisual(Transform keyTransform)
    {
        Transform visual = keyTransform.Find("Visual");
        if (visual != null) return visual;

        foreach (Transform child in keyTransform)
        {
            if (child.GetComponent<Renderer>() != null)
                return child;
        }

        return keyTransform;
    }

    private void PlayRandomKeySound()
    {
        if (keySounds == null || keySounds.Length == 0 || audioSource == null)
            return;

        AudioClip clip = keySounds[Random.Range(0, keySounds.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void RegisterKeyManual(GameObject keyObject)
    {
        if (keyObject == null) return;

        Collider col = keyObject.GetComponent<Collider>();
        if (col != null)
            RegisterKeyData(col, keyObject.transform);
    }

    public void RegisterAllKeysInObject(GameObject parentObject)
    {
        if (parentObject == null) return;

        Collider[] colliders = parentObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null && col.CompareTag("KeyboardKey") && col.isTrigger)
                RegisterKeyData(col, col.transform);
        }
    }

    public void ForceResetAllKeys()
    {
        currentTriggers.Clear();
        registrationOrder.Clear();
        keyDictionary.Clear();
    }
}