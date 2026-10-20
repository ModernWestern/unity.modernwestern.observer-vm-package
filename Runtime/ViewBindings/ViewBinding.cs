using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// One binding configured in the Inspector (View Bindings): a ViewModel member → a widget property.
    /// Resolved once per widget (cached), then it behaves exactly like the same binding written in OnBind.
    /// </summary>
    [Serializable]
    public sealed class ViewBinding
    {
        [SerializeField, Tooltip("Public property or field of the ViewModel: an Observable, IReadOnlyObservable or Command.")]
        private string source;

        [SerializeField, Tooltip("The component (or GameObject) that shows or edits the value.")]
        private UnityEngine.Object target;

        [SerializeField, Tooltip("What to bind on the target, e.g. Fill, Text, Value, Click.")]
        private string property;

        [SerializeField, Tooltip("Optional format for numbers shown as text, e.g. \"{0}\", \"{0:2}\", \"HP {0:0}\".")]
        private string format;

        [SerializeField, Tooltip("For true/false values: use the opposite value.")]
        private bool invert;

        // Cache filled on the first bind (and refreshed if the binding is edited).
        [NonSerialized] private string _resolvedSource;

        [NonSerialized] private string _resolvedProperty;

        [NonSerialized] private UnityEngine.Object _resolvedTarget;

        [NonSerialized] internal Delegate Accessor;

        [NonSerialized] internal ViewBinder Binder;

        [NonSerialized] internal bool Reported;

        public ViewBinding()
        {
        }

        public ViewBinding(string source, UnityEngine.Object target, string property, string format = null, bool invert = false)
        {
            this.source = source;

            this.target = target;

            this.property = property;

            this.format = format;

            this.invert = invert;
        }

        public string Source => source;

        public UnityEngine.Object Target => target;

        public string Property => property;

        public string Format => format;

        public bool Invert => invert;

        public override string ToString()
        {
            var targetName = target != null ? target.name : "(no target)";

            return $"{source} → {targetName}.{property}{(invert ? " (inverted)" : string.Empty)}";
        }

        internal bool IsResolved => Binder != null && string.Equals(_resolvedSource, source) && string.Equals(_resolvedProperty, property) && ReferenceEquals(_resolvedTarget, target);

        internal void MarkResolved(Delegate accessor, ViewBinder binder)
        {
            Accessor = accessor;

            Binder = binder;

            _resolvedSource = source;

            _resolvedProperty = property;

            _resolvedTarget = target;

            Reported = false;
        }
    }
}
