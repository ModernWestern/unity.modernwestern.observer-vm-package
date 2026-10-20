using System;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// Read-only side of an <see cref="Observable{T}"/>. Widgets bound to it can only display the value (ViewModel → View).
    /// </summary>
    public interface IReadOnlyObservable<T>
    {
        T Value { get; }

        /// <summary>
        /// Calls <paramref name="onChanged"/> with <paramref name="state"/> every time the value changes.
        /// Pass a <c>static</c> lambda and the object it needs as <paramref name="state"/> so nothing is captured or allocated.
        /// </summary>
        Subscription Subscribe<TState>(TState state, Action<TState, T> onChanged, bool notifyNow = true) where TState : class;

        /// <inheritdoc cref="Subscribe{TState}"/>
        Subscription Subscribe<TState, TArg>(TState state, TArg arg, Action<TState, TArg, T> onChanged, bool notifyNow = true) where TState : class where TArg : class;
    }

    /// <summary>
    /// A ViewModel property: holds a value and notifies its bindings when it changes.
    /// Changing the value, subscribing and unsubscribing do not allocate once the first ViewModel of a kind was bound.
    /// Not thread-safe: use it from the main thread, like the rest of Unity's UI.
    /// </summary>
    public sealed class Observable<T> : IReadOnlyObservable<T>
    {
        private readonly IEqualityComparer<T> _comparer;

        private readonly SubscriberList<T> _subscribers = new();

        private T _value;

        private int _changeId;

        public Observable() : this(default, null)
        {
        }

        public Observable(T value) : this(value, null)
        {
        }

        public Observable(T value, IEqualityComparer<T> comparer)
        {
            _value = value;

            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (_comparer.Equals(_value, value))
                {
                    return;
                }

                _value = value;

                Notify();
            }
        }

        public int SubscriberCount => _subscribers.Count;

        public void SetValueWithoutNotify(T value)
        {
            _value = value;
        }

        /// <summary>
        /// Pushes the current value to every binding, even if it did not change (e.g. after mutating a list in place).
        /// </summary>
        public void Notify()
        {
            var changeId = ++_changeId;

            var value = _value;

            var count = _subscribers.SlotCount;

            _subscribers.BeginNotify();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    _subscribers.Invoke(i, value);

                    // A binding changed the value again: the nested notification already delivered the newest value to everyone.
                    if (changeId != _changeId)
                    {
                        return;
                    }
                }
            }
            finally
            {
                _subscribers.EndNotify();
            }
        }

        public IReadOnlyObservable<T> AsReadOnly() => this;

        public Subscription Subscribe<TState>(TState state, Action<TState, T> onChanged, bool notifyNow = true) where TState : class
        {
            var subscription = _subscribers.Add(state, onChanged);

            if (notifyNow)
            {
                _subscribers.Invoke(subscription.Index, _value);
            }

            return subscription;
        }

        public Subscription Subscribe<TState, TArg>(TState state, TArg arg, Action<TState, TArg, T> onChanged, bool notifyNow = true) where TState : class where TArg : class
        {
            var subscription = _subscribers.Add(state, arg, onChanged);

            if (notifyNow)
            {
                _subscribers.Invoke(subscription.Index, _value);
            }

            return subscription;
        }

        public override string ToString() => _value?.ToString() ?? "null";
    }
}
