using TMPro;
using UnityEngine;

namespace ModernWestern.UI.ObserverMV.Strategy
{
    public class TextBindingStrategy : IBindingStrategy
    {
        public bool Supports(Component component) => component is TMP_Text;

        public void UpdateBinding(Component component, object value)
        {
            if (component is TMP_Text text && value is string str)
            {
                text.text = str;
            }
        }
    }
}
