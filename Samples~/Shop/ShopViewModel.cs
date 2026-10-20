namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // ViewModel of the whole shop: the wallet and the items on sale.
    public sealed class ShopViewModel : ViewModel
    {
        public ShopViewModel(int gold)
        {
            Wallet = new Wallet(gold);
        }

        public Wallet Wallet { get; }

        public ObservableList<ShopItem> Items { get; } = new();

        public void AddItem(string name, int price)
        {
            Items.Add(new ShopItem(Wallet, name, price));
        }

        protected override void OnDispose()
        {
            foreach (var item in Items)
            {
                item.Dispose();
            }
        }
    }
}
