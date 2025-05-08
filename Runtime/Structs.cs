using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    public readonly struct BindingKey : IEquatable<BindingKey>
    {
        public readonly int Hash;
        public readonly string Key;

        public BindingKey(string key)
        {
            Hash = Animator.StringToHash(key);
            Key = key;
        }

        public override int GetHashCode() => Hash;

        public bool Equals(BindingKey other)
        {
            return Hash == other.Hash;
        }
    }
}
