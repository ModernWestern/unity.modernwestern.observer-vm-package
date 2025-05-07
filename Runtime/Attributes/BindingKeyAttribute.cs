using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BindableAttribute : PropertyAttribute
    {
        public string Key;

        public BindableAttribute(string key = null)
        {
            Key = key;
        }
    }
}
