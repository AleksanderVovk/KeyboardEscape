using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataInitialization : MonoBehaviour
{
    [SerializeField] private Settings _settings;
    [SerializeField] private ValueSystem _valuySystem;
    [SerializeField] private PlayersManager _playersManager;
    [SerializeField] private ItemsManager _itemsManager;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private BrainrotManager _brainrotManager;
    [SerializeField] private SkinManager _skinManager;

    private void Start()
    {
        Initialization();
    }

    private void Initialization()
    {
        if (_settings != null)
            _settings.InitializationSettings();

        if (_valuySystem != null)
            _valuySystem.InitializationBalance();

        if (_brainrotManager != null)
            _brainrotManager.Initialization();

        if (_skinManager != null)
            _skinManager.Initialization();

        if (_playersManager != null)
        {
            _playersManager.ReadNames();
            _playersManager.InitializationAllPlayers();
        }

        if (_itemsManager != null)
            _itemsManager.InitializationItems();

        if (_inventory != null)
            _inventory.InitializeInventory();

        RewardsPanel.instance.OpenDailyReward();

    }
}
