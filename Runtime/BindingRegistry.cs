using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    public class BindingRegistry : IEnumerable<IBindingStrategy>
    {
        private readonly List<IBindingStrategy> _strategies = new();

        public void Add(IBindingStrategy strategy)
        {
            _strategies.Add(strategy);
        }

        public IBindingStrategy GetStrategy(Component component)
        {
            var strategy = _strategies.FirstOrDefault(s => s.Supports(component));

            if (strategy == null)
            {
                Debug.LogWarning($"No strategy found for component {component.GetType().Name}");
            }

            return strategy;
        }

        public IEnumerator<IBindingStrategy> GetEnumerator() => _strategies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
