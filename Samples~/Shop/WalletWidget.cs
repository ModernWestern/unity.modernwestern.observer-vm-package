using TMPro;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    public sealed class WalletWidget : Widget<Wallet>
    {
        [SerializeField, BindWidget] private TMP_Text gold;

        protected override void OnBind(Wallet wallet)
        {
            Bind.Text(gold, wallet.Gold, "{0} G");
        }
    }
}
