using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// Turns the View Bindings of a widget into regular typed bindings. Member access goes through delegates created once per
    /// ViewModel type, and each binding caches what it resolved, so re-binding does not allocate.
    /// </summary>
    internal static class ViewBindingResolver<TViewModel> where TViewModel : class
    {
        private static readonly Dictionary<string, Func<TViewModel, object>> Accessors = new();

        public static void Apply(BindingScope scope, TViewModel viewModel, ViewBinding[] bindings, Widget context)
        {
            if (bindings == null)
            {
                return;
            }

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];

                if (binding == null || !Resolve_Internal(binding, context))
                {
                    continue;
                }

                var source = ((Func<TViewModel, object>)binding.Accessor)(viewModel);

                if (source == null)
                {
                    Report_Internal(binding, context, $"\"{binding.Source}\" is null on this {typeof(TViewModel).Name}.");

                    continue;
                }

                binding.Binder.Bind(scope, binding.Target, source, binding.Format, binding.Invert);
            }
        }

        private static bool Resolve_Internal(ViewBinding binding, Widget context)
        {
            if (binding.IsResolved)
            {
                return true;
            }

            if (binding.Target == null)
            {
                Report_Internal(binding, context, "it has no target widget.");

                return false;
            }

            var source = ViewBindingSource.Find(typeof(TViewModel), binding.Source);

            if (!source.IsValid)
            {
                Report_Internal(binding, context, Application.isEditor
                                                      ? $"{typeof(TViewModel).Name} has no public property or field \"{binding.Source}\" (renamed?)."
                                                      : $"{typeof(TViewModel).Name} has no public property or field \"{binding.Source}\". If it exists in your code, code stripping removed it: mark it with [Preserve] or add it to a link.xml.");

                return false;
            }

            var binder = ViewBinders.Find(binding.Target.GetType(), binding.Property, source);

            if (binder == null)
            {
                Report_Internal(binding, context, $"{binding.Target.GetType().Name} has no \"{binding.Property}\" for {source.Describe()}.");

                return false;
            }

            var accessor = Accessor_Internal(binding.Source);

            if (accessor == null)
            {
                Report_Internal(binding, context, $"\"{binding.Source}\" cannot be read.");

                return false;
            }

            binding.MarkResolved(accessor, binder);

            return true;
        }

        private static Func<TViewModel, object> Accessor_Internal(string member)
        {
            if (Accessors.TryGetValue(member, out var accessor))
            {
                return accessor;
            }

            var property = typeof(TViewModel).GetProperty(member, BindingFlags.Instance | BindingFlags.Public);

            if (property?.GetMethod != null && !property.PropertyType.IsValueType)
            {
                // Open-instance delegate: calling it is a plain method call, no reflection and no allocation.
                accessor = (Func<TViewModel, object>)Delegate.CreateDelegate(typeof(Func<TViewModel, object>), property.GetMethod);
            }
            else if (typeof(TViewModel).GetField(member, BindingFlags.Instance | BindingFlags.Public) is { } field && !field.FieldType.IsValueType)
            {
                accessor = viewModel => field.GetValue(viewModel);
            }

            Accessors[member] = accessor;

            return accessor;
        }

        private static void Report_Internal(ViewBinding binding, Widget context, string problem)
        {
            if (binding.Reported)
            {
                return;
            }

            binding.Reported = true;

            Debug.LogWarning($"[ObserverVM] View binding \"{binding}\" on {(context != null ? context.name : "a widget")} was skipped: {problem}", context);
        }
    }
}
