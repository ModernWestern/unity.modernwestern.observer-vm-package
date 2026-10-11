namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // ViewModel shared by everything that cares about money: the gold counter and every shop item.
    public sealed class Wallet
    {
        public Wallet(int gold)
        {
            Gold = new Observable<int>(gold);
        }

        public Observable<int> Gold { get; }
    }
}
