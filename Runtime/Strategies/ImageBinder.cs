using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverMV.Strategy
{
    public class ImageBindingStrategy : IBindingStrategy
    {
        public bool Supports(Component component) => component is Image;

        public void UpdateBinding(Component component, object value)
        {
            if (component is Image image && value is Sprite sprite)
            {
                image.sprite = sprite;
            }
        }
    }
}
