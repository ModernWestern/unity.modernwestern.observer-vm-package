using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverMV.Strategy
{
    public class ButtonBindingStrategy : IBindingStrategy
    {
        public bool Supports(Component component) => component is Button;

        public void UpdateBinding(Component component, object value)
        {
            if (component is Button button)
            {
                if (value is Color color)
                {
                    button.colors = new ColorBlock
                    {
                        normalColor = color,
                        highlightedColor = color,
                        pressedColor = color,
                        disabledColor = color,
                        colorMultiplier = 1,
                    };
                }

                if (value is Vector3 position)
                {
                    button.transform.position = position;
                }
            }
        }
    }
}
