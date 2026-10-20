using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // One row of the shop. The same prefab shows any item.
    public sealed class ShopItemWidget : Widget<ShopItem>
    {
        [SerializeField, BindWidget] private TMP_Text itemName;

        [SerializeField, BindWidget] private TMP_Text price;

        [SerializeField, BindWidget] private TMP_Text owned;

        [SerializeField, BindWidget] private Button buy;

        protected override void OnBind(ShopItem item)
        {
            // Values that never change need no binding.
            itemName.text = item.Name;

            price.SetText("{0} G", item.Price);

            Bind.Text(owned, item.Owned, "x{0}");

            // Executes the command and greys out by itself while the item is not affordable.
            Bind.Click(buy, item.Buy);
        }
    }
}
