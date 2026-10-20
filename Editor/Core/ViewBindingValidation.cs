using System;
using Object = UnityEngine.Object;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Checks a View Binding against its ViewModel type, like Unreal checking a binding when the Widget Blueprint compiles.
    /// </summary>
    public static class ViewBindingValidation
    {
        public static string Problem(Type viewModelType, ViewBinding binding)
        {
            return binding == null ? "it is empty." : Problem(viewModelType, binding.Source, binding.Target, binding.Property);
        }

        /// <summary>
        /// What is wrong, in plain words, or null when the binding works.
        /// </summary>
        public static string Problem(Type viewModelType, string source, Object target, string property)
        {
            if (string.IsNullOrEmpty(source))
            {
                return "pick a ViewModel member.";
            }

            var found = ViewBindingSource.Find(viewModelType, source);

            if (found.MemberType == null)
            {
                return $"{viewModelType.Name} has no public property or field \"{source}\" (renamed?).";
            }

            if (!found.IsCommand && found.ValueType == null)
            {
                return $"\"{source}\" is a {found.MemberType.Name}: only Observable, IReadOnlyObservable and Command members can be bound.";
            }

            if (target == null)
            {
                return "pick the widget to bind.";
            }

            if (string.IsNullOrEmpty(property))
            {
                return "pick what to bind on the widget.";
            }

            if (ViewBinders.Find(target.GetType(), property, found) == null)
            {
                return $"{target.GetType().Name} has no \"{property}\" for {found.Describe()}.";
            }

            return null;
        }
    }
}
