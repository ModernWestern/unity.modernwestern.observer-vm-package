using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // The shop screen: hands the wallet to its widget and shows one row per item.
    public sealed class ShopWidget : Widget<ShopViewModel>
    {
        [SerializeField, BindWidget] private WalletWidget wallet;

        [SerializeField, BindWidget, Tooltip("Hidden at runtime: one copy is made per item.")]
        private ShopItemWidget rowTemplate;

        protected override void OnBind(ShopViewModel shop)
        {
            Bind.Child(wallet, shop.Wallet);

            // Like UMG's ListView: rows follow the list (add, remove, move) and are pooled.
            Bind.List(rowTemplate, shop.Items);
        }
    }
}
