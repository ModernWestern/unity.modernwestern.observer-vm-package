using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BindableAttribute : PropertyAttribute
    {
        public readonly string Key;

        public readonly bool IsManuallyBound;

        public BindableAttribute(string key)
        {
            IsManuallyBound = false;

            Key = key;
        }

        public BindableAttribute()
        {
            IsManuallyBound = true;
            
            Key = null;
        }
    }
}
