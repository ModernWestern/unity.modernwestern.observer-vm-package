using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverMV
{
    public interface IBindingStrategy
    {
        bool Supports(Component component);

        void UpdateBinding(Component component, object value);
    }

    public interface IBidirectionalBindingStrategy : IBindingStrategy
    {
        void BindViewEvent(Component component, Action<string, object> onViewChanged, string key);
    }
}
