using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BindableAttribute : PropertyAttribute
    {
        public readonly BindingKey BindingKey;

        public bool HasBindingKey { get; }

        public string Key { get; }

        public BindableAttribute(string key)
        {
            HasBindingKey = false;

            Key = key;
        }

        public BindableAttribute(BindingKey bindingKey)
        {
            BindingKey = bindingKey;

            HasBindingKey = true;

            Key = bindingKey.Key;
        }
    }
}
