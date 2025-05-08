using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BindableAttribute : PropertyAttribute
    {
        public readonly string Key;

        public BindableAttribute(string key)
        {
            Key = key;
        }
    }
}
