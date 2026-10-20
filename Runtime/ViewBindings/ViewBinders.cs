using System;
using System.Reflection;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace ModernWestern.UI.ObserverVM
{
    [Flags]
    public enum ViewBinderFeatures
    {
        None = 0,

        /// <summary>
        /// Accepts a number format ("{0}", "{0:2}").
        /// </summary>
        Format = 1,

        /// <summary>
        /// Accepts "invert" (true/false values).
        /// </summary>
        Invert = 2,

        /// <summary>
        /// Writes back to an Observable when the user edits the widget.
        /// </summary>
        TwoWay = 4
    }

    /// <summary>
    /// A widget property that View Bindings can target, e.g. "Fill" on an Image for a float. Register your own with <see cref="ViewBinders"/>.
    /// </summary>
    public abstract class ViewBinder
    {
        protected ViewBinder(string id, Type targetType, Type valueType, ViewBinderFeatures features)
        {
            Id = id;

            TargetType = targetType;

            ValueType = valueType;

            Features = features;
        }

        public string Id { get; }

        public Type TargetType { get; }

        /// <summary>
        /// The value type it shows, or null for a Command.
        /// </summary>
        public Type ValueType { get; }

        public ViewBinderFeatures Features { get; }

        public bool IsCommand => ValueType == null;

        public bool Has(ViewBinderFeatures feature) => (Features & feature) == feature;

        public bool Accepts(Type targetType, ViewBindingSource source)
        {
            return TargetType.IsAssignableFrom(targetType) && (IsCommand ? source.IsCommand : !source.IsCommand && ValueType == source.ValueType);
        }

        internal abstract bool Bind(BindingScope scope, Object target, object source, string format, bool invert);
    }

    /// <summary>
    /// What a ViewModel member offers to View Bindings: a value (with its type, writable or not) or a Command.
    /// </summary>
    public readonly struct ViewBindingSource
    {
        private ViewBindingSource(string name, Type memberType, Type valueType, bool writable, bool isCommand)
        {
            Name = name;

            MemberType = memberType;

            ValueType = valueType;

            Writable = writable;

            IsCommand = isCommand;
        }

        public string Name { get; }

        public Type MemberType { get; }

        public Type ValueType { get; }

        public bool Writable { get; }

        public bool IsCommand { get; }

        public bool IsValid => MemberType != null;

        /// <summary>
        /// Public instance properties and fields of a ViewModel type that View Bindings can use.
        /// </summary>
        public static List<ViewBindingSource> Of(Type viewModelType)
        {
            var sources = new List<ViewBindingSource>();

            if (viewModelType == null)
            {
                return sources;
            }

            foreach (var property in viewModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0 && TryDescribe(property.Name, property.PropertyType, out var source))
                {
                    sources.Add(source);
                }
            }

            foreach (var field in viewModelType.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (TryDescribe(field.Name, field.FieldType, out var source))
                {
                    sources.Add(source);
                }
            }

            return sources;
        }

        public static ViewBindingSource Find(Type viewModelType, string name)
        {
            if (viewModelType == null || string.IsNullOrEmpty(name))
            {
                return default;
            }

            var property = viewModelType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                return TryDescribe(name, property.PropertyType, out var fromProperty) ? fromProperty : new ViewBindingSource(name, property.PropertyType, null, false, false);
            }

            var field = viewModelType.GetField(name, BindingFlags.Instance | BindingFlags.Public);

            if (field != null)
            {
                return TryDescribe(name, field.FieldType, out var fromField) ? fromField : new ViewBindingSource(name, field.FieldType, null, false, false);
            }

            return default;
        }

        /// <summary>
        /// Observable&lt;T&gt; (writable value), IReadOnlyObservable&lt;T&gt; (read-only value) or Command.
        /// </summary>
        public static bool TryDescribe(string name, Type memberType, out ViewBindingSource source)
        {
            if (memberType == typeof(Command))
            {
                source = new ViewBindingSource(name, memberType, null, false, true);

                return true;
            }

            if (memberType.IsGenericType)
            {
                var definition = memberType.GetGenericTypeDefinition();

                if (definition == typeof(Observable<>) || definition == typeof(IReadOnlyObservable<>))
                {
                    source = new ViewBindingSource(name, memberType, memberType.GetGenericArguments()[0], definition == typeof(Observable<>), false);

                    return true;
                }
            }

            source = default;

            return false;
        }

        /// <summary>
        /// "Health (float)", "Volume (float, writable)", "Respawn (Command)".
        /// </summary>
        public string Describe()
        {
            if (IsCommand)
            {
                return $"{Name} (Command)";
            }

            return ValueType == null ? $"{Name} ({MemberType?.Name})" : $"{Name} ({TypeName(ValueType)}{(Writable ? ", writable" : string.Empty)})";
        }

        public static string TypeName(Type type)
        {
            if (type == typeof(float)) return "float";
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";

            return type.Name;
        }
    }

    /// <summary>
    /// Registry of the widget properties View Bindings can target. UGUI and TextMeshPro register theirs automatically;
    /// add yours from a [RuntimeInitializeOnLoadMethod] (and [InitializeOnLoadMethod] in the Editor) with a static lambda:
    /// <c>ViewBinders.Register&lt;RectTransform, float&gt;("Width", static (bind, rect, width) => bind.Width(rect, width));</c>
    /// </summary>
    public static class ViewBinders
    {
        private static readonly List<ViewBinder> Binders = new();

        public static IReadOnlyList<ViewBinder> All => Binders;

        public static void Register<TTarget, T>(string id,
                                                Action<BindingScope, TTarget, IReadOnlyObservable<T>> bind,
                                                Action<BindingScope, TTarget, Observable<T>> twoWay = null,
                                                Action<BindingScope, TTarget, IReadOnlyObservable<T>> inverted = null,
                                                Action<BindingScope, TTarget, IReadOnlyObservable<T>, string> formatted = null) where TTarget : Object
        {
            Add_Internal(new ValueViewBinder<TTarget, T>(id, bind, twoWay, inverted, formatted));
        }

        public static void RegisterCommand<TTarget>(string id, Action<BindingScope, TTarget, Command> bind) where TTarget : Object
        {
            Add_Internal(new CommandViewBinder<TTarget>(id, bind));
        }

        /// <summary>
        /// The binder for <paramref name="id"/> on <paramref name="targetType"/> that accepts <paramref name="source"/>, or null.
        /// </summary>
        public static ViewBinder Find(Type targetType, string id, ViewBindingSource source)
        {
            if (targetType == null || string.IsNullOrEmpty(id) || !source.IsValid)
            {
                return null;
            }

            // Most specific target type wins (e.g. TMP_Text "Text" over Graphic).
            ViewBinder best = null;

            foreach (var binder in Binders)
            {
                if (binder.Id == id && binder.Accepts(targetType, source) && (best == null || best.TargetType.IsAssignableFrom(binder.TargetType)))
                {
                    best = binder;
                }
            }

            return best;
        }

        /// <summary>
        /// Every binder usable with a target of <paramref name="targetType"/> and <paramref name="source"/>.
        /// </summary>
        public static List<ViewBinder> For(Type targetType, ViewBindingSource source)
        {
            var found = new List<ViewBinder>();

            foreach (var binder in Binders)
            {
                if (binder.Accepts(targetType, source) && found.TrueForAll(other => other.Id != binder.Id))
                {
                    found.Add(Find(targetType, binder.Id, source));
                }
            }

            return found;
        }

        private static void Add_Internal(ViewBinder binder)
        {
            // Registering twice (e.g. after a domain reload) replaces the previous one.
            Binders.RemoveAll(other => other.Id == binder.Id && other.TargetType == binder.TargetType && other.ValueType == binder.ValueType);

            Binders.Add(binder);
        }

        private sealed class ValueViewBinder<TTarget, T> : ViewBinder where TTarget : Object
        {
            private readonly Action<BindingScope, TTarget, IReadOnlyObservable<T>> _bind;

            private readonly Action<BindingScope, TTarget, Observable<T>> _twoWay;

            private readonly Action<BindingScope, TTarget, IReadOnlyObservable<T>> _inverted;

            private readonly Action<BindingScope, TTarget, IReadOnlyObservable<T>, string> _formatted;

            public ValueViewBinder(string id,
                                   Action<BindingScope, TTarget, IReadOnlyObservable<T>> bind,
                                   Action<BindingScope, TTarget, Observable<T>> twoWay,
                                   Action<BindingScope, TTarget, IReadOnlyObservable<T>> inverted,
                                   Action<BindingScope, TTarget, IReadOnlyObservable<T>, string> formatted)
                : base(id, typeof(TTarget), typeof(T),
                       (twoWay != null ? ViewBinderFeatures.TwoWay : ViewBinderFeatures.None) |
                       (inverted != null ? ViewBinderFeatures.Invert : ViewBinderFeatures.None) |
                       (formatted != null ? ViewBinderFeatures.Format : ViewBinderFeatures.None))
            {
                _bind = bind ?? throw new ArgumentNullException(nameof(bind));

                _twoWay = twoWay;

                _inverted = inverted;

                _formatted = formatted;
            }

            internal override bool Bind(BindingScope scope, Object target, object source, string format, bool invert)
            {
                if (target is not TTarget typedTarget || source is not IReadOnlyObservable<T> observable)
                {
                    return false;
                }

                if (invert && _inverted != null)
                {
                    _inverted(scope, typedTarget, observable);
                }
                else if (_twoWay != null && source is Observable<T> writable)
                {
                    _twoWay(scope, typedTarget, writable);
                }
                else if (_formatted != null && !string.IsNullOrEmpty(format))
                {
                    _formatted(scope, typedTarget, observable, format);
                }
                else
                {
                    _bind(scope, typedTarget, observable);
                }

                return true;
            }
        }

        private sealed class CommandViewBinder<TTarget> : ViewBinder where TTarget : Object
        {
            private readonly Action<BindingScope, TTarget, Command> _bind;

            public CommandViewBinder(string id, Action<BindingScope, TTarget, Command> bind) : base(id, typeof(TTarget), null, ViewBinderFeatures.None)
            {
                _bind = bind ?? throw new ArgumentNullException(nameof(bind));
            }

            internal override bool Bind(BindingScope scope, Object target, object source, string format, bool invert)
            {
                if (target is not TTarget typedTarget || source is not Command command)
                {
                    return false;
                }

                _bind(scope, typedTarget, command);

                return true;
            }
        }
    }
}
