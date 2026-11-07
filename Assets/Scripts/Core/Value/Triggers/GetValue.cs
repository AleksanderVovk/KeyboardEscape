using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GetValue : MonoBehaviour
{
    public Price price;
    [SerializeField] private TMP_Text _display;

    private void Start()
    {
        if (_display.text != null)
        {
            switch (price.valueType)
            {
                case ValueType.Wins:
                    _display.text = "+" + price.amount + " " + UTF.ReadLanguageDirectory(LangList.instance.win) + " " + UTF.manager.TextToSprite("\U0001F600");
                    break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player.thisRealPlayer)
            {
                switch (price.valueType)
                {
                    case ValueType.Wins:
                        NotificationManager.ShowNotification("+" + price.amount + " " + UTF.ReadLanguageDirectory(LangList.instance.won) + "!" + " " + UTF.manager.TextToSprite("\U0001F600"), NotificationType.Win);
                        Debug.Log("Show win notification!");
                        break;
                }

                ValueSystem.AddBalance(price.amount, price.valueType);
                player.TeleportToSpawn();
            }
        }
    }
}
