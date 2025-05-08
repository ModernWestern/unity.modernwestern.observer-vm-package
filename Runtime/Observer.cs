using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    [CreateAssetMenu(menuName = "ModernWestern/ObserverVM/Observer")]
    public class Observer : ScriptableObject
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Dictionary<string, Component> _stringBindings = new();

        private readonly Dictionary<int, Component> _hashBindings = new();

        private Action<string, object> _onViewChanged;

        private static BindingRegistry _registry;

        public static void Init(BindingRegistry registry)
        {
            _registry = registry;
        }

        public static void DeInit()
        {
            _registry = null;
        }

        public void Bind(BindingKey key, Component target)
        {
            _hashBindings[key.GetHashCode()] = target;
        }

        public void Bind(string key, Component target)
        {
            _stringBindings[key] = target;
        }

        public void AutoBind(MonoBehaviour target)
        {
            _hashBindings.Clear();

            _stringBindings.Clear();

            var fields = target.GetType().GetFields(Flags);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<BindableAttribute>() is not { } attribute)
                {
                    continue;
                }

                if (field.GetValue(target) is not Component component)
                {
                    continue;
                }

                var key = string.IsNullOrEmpty(attribute.Key) ? field.Name : attribute.Key;

                if (attribute.HasBindingKey)
                {
                    Bind(attribute.BindingKey, component);
                }
                else
                {
                    Bind(key, component);
                }

                var strategy = _registry?.GetStrategy(component);

                if (strategy is IBidirectionalBindingStrategy bidirectionalStrategy)
                {
                    bidirectionalStrategy?.BindViewEvent(component, OnViewChangedInternal, key);
                }
            }
        }

        public void UpdateValue(BindingKey key, object value)
        {
            if (!_hashBindings.TryGetValue(key.GetHashCode(), out var target))
            {
                return;
            }

            var strategy = _registry?.GetStrategy(target);

            strategy?.UpdateBinding(target, value);
        }

        public void UpdateValue(string key, object value)
        {
            if (!_stringBindings.TryGetValue(key, out var component))
            {
                return;
            }

            var strategy = _registry?.GetStrategy(component);

            strategy?.UpdateBinding(component, value);
        }

        public void SubscribeViewChanges(Action<string, object> callback)
        {
            _onViewChanged = callback;
        }

        private void OnViewChangedInternal(string key, object value)
        {
            _onViewChanged?.Invoke(key, value);
        }
    }
}
