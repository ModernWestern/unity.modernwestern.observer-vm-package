using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // The "game": builds the shop ViewModel from data, hands it to the shop widget and earns gold over time.
    // It never touches the UI after that: changing the ViewModel is enough for every widget to update.
    public sealed class ShopDemo : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public string name;

            public int price;
        }

        [SerializeField] private ShopWidget shop;

        [SerializeField] private int startingGold = 100;

        [SerializeField] private int incomePerSecond = 10;

        [SerializeField] private Entry[] catalog =
        {
            new() { name = "Potion", price = 25 },
            new() { name = "Shield", price = 80 },
            new() { name = "Sword", price = 150 },
            new() { name = "Dragon Egg", price = 500 }
        };

        private ShopViewModel _shop;

        private float _incomeTimer;

        private void Awake()
        {
            _shop = new ShopViewModel(startingGold);

            foreach (var entry in catalog)
            {
                _shop.AddItem(entry.name, entry.price);
            }

            shop.SetViewModel(_shop);
        }

        private void Update()
        {
            _incomeTimer += Time.deltaTime;

            if (_incomeTimer < 1f)
            {
                return;
            }

            _incomeTimer -= 1f;

            _shop.Wallet.Gold.Value += incomePerSecond;
        }

        private void OnDestroy()
        {
            _shop?.Dispose();
        }
    }
}
