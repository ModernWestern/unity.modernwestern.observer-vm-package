namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // ViewModel of one item. It knows the wallet, never the UI.
    public sealed class ShopItem : ViewModel
    {
        private readonly Wallet _wallet;

        public ShopItem(Wallet wallet, string name, int price)
        {
            _wallet = wallet;

            Name = name;

            Price = price;

            Buy = new Command(OnBuy);

            // Derived state: "can I afford it?" follows the wallet by itself, whoever changes the gold.
            // Watch stops when the item is disposed, so a removed item does not keep listening to the wallet.
            Watch(wallet.Gold, this, static (item, gold) => item.Buy.CanExecute.Value = gold >= item.Price);
        }

        public string Name { get; }

        public int Price { get; }

        public Observable<int> Owned { get; } = new();

        public Command Buy { get; }

        public override string ToString() => Name;

        private void OnBuy()
        {
            _wallet.Gold.Value -= Price;

            Owned.Value++;
        }
    }
}
