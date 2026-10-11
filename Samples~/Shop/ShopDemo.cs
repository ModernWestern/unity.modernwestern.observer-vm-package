using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // The "game": builds the ViewModels from data, hands them to the widgets and earns gold over time.
    // It never touches the UI after that: changing the wallet is enough for every widget to update.
    public sealed class ShopDemo : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public string name;

            public int price;
        }

        [SerializeField] private WalletWidget wallet;

        [SerializeField, Tooltip("Row used as a template: one copy is created per catalog entry.")]
        private ShopItemWidget rowTemplate;

        [SerializeField] private int startingGold = 100;

        [SerializeField] private int incomePerSecond = 10;

        [SerializeField] private Entry[] catalog =
        {
            new() { name = "Potion", price = 25 },
            new() { name = "Shield", price = 80 },
            new() { name = "Sword", price = 150 },
            new() { name = "Dragon Egg", price = 500 }
        };

        private Wallet _wallet;

        private float _incomeTimer;

        private void Awake()
        {
            _wallet = new Wallet(startingGold);

            wallet.SetViewModel(_wallet);

            foreach (var entry in catalog)
            {
                var row = Instantiate(rowTemplate, rowTemplate.transform.parent);

                row.name = entry.name;

                row.gameObject.SetActive(true);

                row.SetViewModel(new ShopItem(_wallet, entry.name, entry.price));
            }

            rowTemplate.gameObject.SetActive(false);
        }

        private void Update()
        {
            _incomeTimer += Time.deltaTime;

            if (_incomeTimer < 1f)
            {
                return;
            }

            _incomeTimer -= 1f;

            _wallet.Gold.Value += incomePerSecond;
        }
    }
}
