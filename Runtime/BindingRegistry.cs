using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    public class BindingRegistry
    {
        private readonly List<IBindingStrategy> _strategies = new();

        public BindingRegistry(IBindingStrategy strategy)
        {
            Register(strategy);
        }

        public BindingRegistry(params IBindingStrategy[] strategies)
        {
            Register(strategies);
        }

        public void Register(IBindingStrategy strategy)
        {
            _strategies.Add(strategy);
        }

        public void Register(params IBindingStrategy[] strategies)
        {
            _strategies.AddRange(strategies);
        }

        public IBindingStrategy GetStrategy(Component component)
        {
            foreach (var strategy in _strategies.Where(strategy => strategy.Supports(component)))
            {
                return strategy;
            }

            Debug.LogWarning($"No strategy found for component {component.GetType().Name}");

            return null;
        }
    }
}
