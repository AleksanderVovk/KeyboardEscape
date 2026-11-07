using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public bool isFire;
    public bool onFire; // Сущность находиться в огне - копим урон

    public Player player;

    [SerializeField] private PlayerState _playerState;

    public void Death()
    {
        if (_playerState != null)
        {
            if (_playerState.isDeath == false)
            {
                player.animator.SetTrigger("Death");

                StopAllCoroutines();
                StartCoroutine(DeathMethod());
            }
        }
    }

    private IEnumerator DeathMethod()
    {
        if (_playerState != null)
            _playerState.isDeath = true;

        player.animator.SetBool("isDeath", _playerState.isDeath);

        isFire = false;
        onFire = false;

        yield return new WaitForSeconds(2f);

        _playerState.isDeath = false;

        player.Respawn();
        player.animator.SetBool("isDeath", _playerState.isDeath);

        yield break;
    }
}
