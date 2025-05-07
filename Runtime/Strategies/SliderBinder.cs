using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverMV.Strategy
{
    public class SliderBindingStrategy : IBidirectionalBindingStrategy
    {
        public bool Supports(Component component) => component is Slider;

        public void UpdateBinding(Component component, object value)
        {
            if (component is Slider slider && value is float f)
            {
                slider.SetValueWithoutNotify(f);
            }
        }
        public void BindViewEvent(Component component, Action<string, object> onViewChanged, string key)
        {
            if (component is Slider slider)
            {
                slider.onValueChanged.AddListener(value =>
                {
                    onViewChanged?.Invoke(key, value);
                });
            }
        }
    }
}
