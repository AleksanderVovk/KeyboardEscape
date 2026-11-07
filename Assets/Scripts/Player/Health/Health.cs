using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public Entity entity;

    public float currentHealth;
    public float maxHealth = 100;

    [SerializeField] Slider _healthSlider;

    [Header("Огонь")]
    [Space(2.5f)]
    public float accumulatedFireDamage; // Накопленный урон от огня
    public float accumulatedMaxFireDamage = 100f;
    public float fireDamage = 7.5f;

    [SerializeField] private float _fireDamageDelay = 1f;
    [SerializeField] private float _lastFireDamageDelay;

    [SerializeField] private ParticleSystem _fireParticle;

    [SerializeField] private BuffObject _fireHealthObject;
    [SerializeField] private BuffObject[] _allBuffsObjects;

    [SerializeField] private AudioClip[] _hitClips;
    [SerializeField] private AudioSource _audioSource;

    private void Start()
    {
        currentHealth = maxHealth;

        _healthSlider.maxValue = maxHealth;
        _healthSlider.value = currentHealth;

        _healthSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        AddFire();
        FireHolder();
    }

    private void AddFire()
    {
        if (entity.onFire)
        {
            accumulatedFireDamage += 4f * Time.deltaTime;

            if (accumulatedFireDamage > accumulatedMaxFireDamage)
                accumulatedFireDamage = accumulatedMaxFireDamage;
        }
    }

    private void FireHolder()
    {
        if(accumulatedFireDamage > 0)
        {
            if (!entity.isFire)
            {
                entity.isFire = true;

                _ShowHealthBar();
                _fireHealthObject.Show();

                _fireParticle.Play();
            }

            accumulatedFireDamage -= 2 * Time.deltaTime;

            if (_fireDamageDelay + _lastFireDamageDelay < Time.time)
            {
                HurtDamage(fireDamage);
                _fireHealthObject.Hit();

                _lastFireDamageDelay = Time.time;
            }
        }
        else
        {
            if (entity.isFire)
            {
                entity.isFire = false;

                _ShowHealthBar();
                ClearFireDamage();
            }
        }
    }

    private void ClearFireDamage()
    {
        _fireHealthObject.Hide();

        _fireParticle.Stop();
        accumulatedFireDamage = 0;
    }

    private void _ShowHealthBar()
    {
        StopAllCoroutines();
        StartCoroutine(ShowHealthBar());
    }

    public void HurtDamage(float damage)
    {
        currentHealth -= damage;
        _healthSlider.value = currentHealth;

        _ShowHealthBar();

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            // DEATH
            entity.Death();
            ClearFireDamage();
            RefreshHealth();
        }
        else
        {
            _audioSource.PlayOneShot(_hitClips[Random.Range(0, _hitClips.Length)]);
            // Звук получения урона
        }
    }

    private void RefreshHealth()
    {
        currentHealth = maxHealth;
        _healthSlider.value = currentHealth;
    }

    private IEnumerator ShowHealthBar()
    {
        _healthSlider.gameObject.SetActive(true);

        while (true) // Бесконечный цикл, который ты сможешь контролировать
        {
            bool anyBuffVisible = false;

            // Проходим по всем объектам
            foreach (var buff in _allBuffsObjects)
            {
                // Проверяем, есть ли активный Buff
                if (buff.isShow)
                {
                    anyBuffVisible = true; // Если хоть один видим, то устанавливаем true
                    break; // Выходим из цикла, если нашли активный
                }
            }

            // Если все Buff не видны, выходим из корутины
            if (!anyBuffVisible)
            {                
                yield return new WaitForSeconds(2f); // Завершаем корутину

                _healthSlider.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(0.1f); // Ждем перед следующей проверкой
        }
    }
}
