using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISound : MonoBehaviour
{
    public static UISound manager;

    [SerializeField] private AudioClip _clickSonud;
    [SerializeField] private AudioClip _clickShortSound;
    [SerializeField] private AudioClip _closeSound;
    [SerializeField] private AudioClip _sucsessfulSonud;
    [SerializeField] private AudioClip _popSound;
    [SerializeField] private AudioClip[] _errorSound;
    [SerializeField] private AudioClip[] _buySound;
    [SerializeField] private AudioClip _getMoneySound;
    [SerializeField] private AudioClip _upgradeSound;
    [SerializeField] private AudioClip _teleportSound;
    [SerializeField] private AudioClip[] _hitSound;
    [SerializeField] private AudioClip _fanfaraSound;
    [SerializeField] private AudioClip _levelUpSound;

    [SerializeField] private AudioClip _commonDrop;
    [SerializeField] private AudioClip _rareDrop;
    [SerializeField] private AudioClip _legendaryDrop;

    [SerializeField] private AudioClip _openDailyRewardSound;

    private AudioSource _audioSource;

    private void Awake()
    {
        manager = this;

        _audioSource = GetComponent<AudioSource>();
    }

    public void PlayClick(bool changePitch)
    {
        PlaySound(_clickSonud, changePitch);
    }

    public void PlayClickShort(bool changePitch)
    {
        PlaySound(_clickShortSound, changePitch);
    }

    public void PlayClose(bool changePitch)
    {
        PlaySound(_closeSound, changePitch);
    }

    public void PlaySucsessful(bool changePitch)
    {
        PlaySound(_sucsessfulSonud, changePitch);
    }

    public void PlayPop(bool changePitch)
    {
        PlaySound(_popSound, changePitch);
    }

    public void PlayError(bool changePitch)
    {
        PlaySound(_errorSound[Random.Range(0, _errorSound.Length)], changePitch);
    }

    public void PlayBuy(bool changePitch)
    {
        PlaySound(_buySound[Random.Range(0, _buySound.Length)], changePitch);
    }

    public void PlayGetMoney(bool changePitch)
    {
        PlaySound(_getMoneySound, changePitch);
    }

    public void PlayUpgrade(bool changePitch)
    {
        PlaySound(_upgradeSound, changePitch);
    }

    public void PlayTeleport(bool changePitch)
    {
        PlaySound(_teleportSound, changePitch);
    }

    public void PlayHit(bool changePitch)
    {
        PlaySound(_hitSound[Random.Range(0, _hitSound.Length)], changePitch);
    }

    public void PlayWin(bool changePitch)
    {
        PlaySound(_fanfaraSound, changePitch);
    }

    public void PlayOpenDailyReward(bool changePitch)
    {
        PlaySound(_openDailyRewardSound, changePitch);
    }

    public void PlayLevelUp(bool changePitch)
    {
        PlaySound(_levelUpSound, changePitch);
    }

    public void PlaySound(AudioClip audioClip, bool changePitch)
    {
        float pitch = 1f;

        if (changePitch)
            pitch = Random.Range(0.9f, 1f);

        _audioSource.pitch = pitch;

        _audioSource.PlayOneShot(audioClip);
    }
}
