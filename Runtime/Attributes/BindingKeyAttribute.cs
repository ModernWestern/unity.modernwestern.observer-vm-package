using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    public class BindableAttribute : PropertyAttribute
    {
        public bool IsManuallyBound => string.IsNullOrEmpty(Key);

        public readonly string Key;

        public BindableAttribute(string key = null)
        {
            Key = key;
        }
    }
}
