namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // ViewModel of one item. It knows the wallet, never the UI.
    public sealed class ShopItem
    {
        private readonly Wallet _wallet;

        public ShopItem(Wallet wallet, string name, int price)
        {
            _wallet = wallet;

            Name = name;

            Price = price;

            Buy = new Command(OnBuy);

            // Derived state: "can I afford it?" follows the wallet by itself, whoever changes the gold.
            wallet.Gold.Subscribe(this, static (item, gold) => item.Buy.CanExecute.Value = gold >= item.Price);
        }

        public string Name { get; }

        public int Price { get; }

        public Observable<int> Owned { get; } = new();

        public Command Buy { get; }

        private void OnBuy()
        {
            _wallet.Gold.Value -= Price;

            Owned.Value++;
        }
    }
}
