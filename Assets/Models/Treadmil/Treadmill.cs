using TMPro;
using UnityEngine;
using YG;

public class Treadmill : MonoBehaviour
{
    [Header("Purchase")]
    [Tooltip("ID товара из Payments Catalog")]
    public string purchaseId;

    public LanguageDirectory treadmillRare;
    public Color treadmillRareColor;

    [Header("XP")]
    [Min(1f)]
    public double xpMultiplier = 2.0;

    private bool _playerInside;

    public bool IsPlayerInside => _playerInside;

    [SerializeField] private TMP_Text _speedMultiplierDisplay;

    private void Start()
    {
        RefreshPurchasedState();

        _speedMultiplierDisplay.text = "x" + xpMultiplier + " " + UTF.ReadLanguageDirectory(LangList.instance.speed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsRealPlayer(other))
            return;

        _playerInside = true;

        if (IsPurchased())
        {
            ActivateForPlayer();
        }
        else
        {
            DeactivateForPlayer();

            if (TreadmillPurchaseWindow.Instance != null)
            {
                TreadmillPurchaseWindow.Instance.Show(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsRealPlayer(other))
            return;

        _playerInside = false;
        DeactivateForPlayer();

        if (TreadmillPurchaseWindow.Instance != null)
            TreadmillPurchaseWindow.Instance.Close();
    }

    public void ActivateForPlayer()
    {
        if (!IsPurchased())
            return;

        Player player = GetRealPlayer();

        if (player == null || player.playerState == null)
            return;

        player.playerState.playerInTrademill = true;

        if (PlayerSpeedLevelManager.instance != null)
        {
            PlayerSpeedLevelManager.instance.SetTreadmillXpMultiplier(
                xpMultiplier
            );

            PlayerSpeedLevelManager.instance.EnterTreadmill();
        }

        Debug.Log(
            $"[Treadmill] Activated: {purchaseId}, XP x{xpMultiplier:F2}"
        );
    }

    public void DeactivateForPlayer()
    {
        Player player = GetRealPlayer();

        if (player != null && player.playerState != null)
            player.playerState.playerInTrademill = false;

        if (PlayerSpeedLevelManager.instance != null)
        {
            PlayerSpeedLevelManager.instance.ExitTreadmill();
            PlayerSpeedLevelManager.instance.SetTreadmillXpMultiplier(1.0);
        }
    }

    public void MarkPurchased()
    {
        RefreshPurchasedState();
    }

    private bool IsPurchased()
    {
        return TreadmillPurchaseManager.Instance != null &&
               TreadmillPurchaseManager.Instance.IsPurchased(purchaseId);
    }

    private void RefreshPurchasedState()
    {
        bool purchased = IsPurchased();

        Debug.Log(
            $"[Treadmill] {purchaseId}: purchased = {purchased}"
        );
    }

    private bool IsRealPlayer(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        return player != null && player.thisRealPlayer;
    }

    private Player GetRealPlayer()
    {
        if (PlayersManager.instance == null)
            return null;

        if (PlayersManager.instance.realPlayer == null)
            return null;

        return PlayersManager.instance.realPlayer;
    }
}