using System;
using UnityEngine;
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
    public sealed class Observable<T> : IReadOnlyObservable<T>, ISubscriptionSource
    {
        private delegate void Invoker(Delegate callback, object state, object arg, T value);

        private struct Slot
        {
            public object State;

            public object Arg;

            public Delegate Callback;

            public Invoker Invoke;

            public int Version;

            public int NextFree;
        }

        private static class Thunk<TState> where TState : class
        {
            public static readonly Invoker Invoke = (callback, state, _, value) => ((Action<TState, T>)callback)((TState)state, value);
        }

        private static class Thunk<TState, TArg> where TState : class where TArg : class
        {
            public static readonly Invoker Invoke = (callback, state, arg, value) => ((Action<TState, TArg, T>)callback)((TState)state, (TArg)arg, value);
        }

        // Arrays for properties with 2+ bindings are shared per T: unbinding a ViewModel hands them to the next one.
        private static class SlotPool
        {
            private const int MinLength = 4;

            private const int MaxPooledPerLength = 32;

            private static readonly Stack<Slot[]>[] Buckets =
            {
                new(MaxPooledPerLength), new(MaxPooledPerLength), new(MaxPooledPerLength),
                new(MaxPooledPerLength), new(MaxPooledPerLength), new(MaxPooledPerLength)
            };

            public static Slot[] Rent(int minimumLength)
            {
                var bucket = BucketOf_Internal(minimumLength, out var length);

                return bucket >= 0 && Buckets[bucket].Count > 0 ? Buckets[bucket].Pop() : new Slot[length];
            }

            public static void Return(Slot[] slots)
            {
                Array.Clear(slots, 0, slots.Length);

                var bucket = BucketOf_Internal(slots.Length, out var length);

                if (bucket >= 0 && length == slots.Length && Buckets[bucket].Count < MaxPooledPerLength)
                {
                    Buckets[bucket].Push(slots);
                }
            }

            private static int BucketOf_Internal(int minimumLength, out int length)
            {
                length = MinLength;

                var bucket = 0;

                while (length < minimumLength)
                {
                    length <<= 1;

                    bucket++;
                }

                return bucket < Buckets.Length ? bucket : -1;
            }
        }

        private readonly IEqualityComparer<T> _comparer;

        private T _value;

        // Most properties have a single binding, so the first slot lives inline and only extra bindings need an array.
        private Slot _firstSlot;

        private Slot[] _extraSlots = Array.Empty<Slot>();

        private int _slotCount;

        private int _freeSlot = -1;

        private int _lastVersion;

        private int _changeId;

        private int _notifyDepth;

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

        public int SubscriberCount { get; private set; }

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

            var count = _slotCount;

            _notifyDepth++;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var slot = SlotAt_Internal(i);

                    if (slot.Callback == null)
                    {
                        continue;
                    }

                    Invoke_Internal(slot.Invoke, slot.Callback, slot.State, slot.Arg, value);

                    // A binding changed the value again: the nested notification already delivered the newest value to everyone.
                    if (changeId != _changeId)
                    {
                        return;
                    }
                }
            }
            finally
            {
                _notifyDepth--;

                ReleaseIfUnused_Internal();
            }
        }

        public IReadOnlyObservable<T> AsReadOnly() => this;

        public Subscription Subscribe<TState>(TState state, Action<TState, T> onChanged, bool notifyNow = true) where TState : class
        {
            return Add_Internal(state, null, onChanged, Thunk<TState>.Invoke, notifyNow);
        }

        public Subscription Subscribe<TState, TArg>(TState state, TArg arg, Action<TState, TArg, T> onChanged, bool notifyNow = true) where TState : class where TArg : class
        {
            return Add_Internal(state, arg, onChanged, Thunk<TState, TArg>.Invoke, notifyNow);
        }

        public override string ToString() => _value?.ToString() ?? "null";

#region Internal

        void ISubscriptionSource.Unsubscribe(int index, int version)
        {
            if ((uint)index >= (uint)_slotCount)
            {
                return;
            }

            ref var slot = ref SlotAt_Internal(index);

            if (slot.Version != version || slot.Callback == null)
            {
                return;
            }

            slot = new Slot
            {
                NextFree = _freeSlot
            };

            _freeSlot = index;

            SubscriberCount--;

            ReleaseIfUnused_Internal();
        }

        bool ISubscriptionSource.IsSubscribed(int index, int version)
        {
            if ((uint)index >= (uint)_slotCount)
            {
                return false;
            }

            ref var slot = ref SlotAt_Internal(index);

            return slot.Version == version && slot.Callback != null;
        }

        private Subscription Add_Internal(object state, object arg, Delegate callback, Invoker invoke, bool notifyNow)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            int index;

            if (_freeSlot >= 0)
            {
                index = _freeSlot;

                _freeSlot = SlotAt_Internal(index).NextFree;
            }
            else
            {
                if (_slotCount > _extraSlots.Length)
                {
                    Grow_Internal();
                }

                index = _slotCount++;
            }

            ref var slot = ref SlotAt_Internal(index);

            slot.State = state;

            slot.Arg = arg;

            slot.Callback = callback;

            slot.Invoke = invoke;

            slot.NextFree = -1;

            // Versions are unique per observable, so a stale Subscription can never remove whoever reuses its slot.
            slot.Version = ++_lastVersion;

            SubscriberCount++;

            var subscription = new Subscription(this, index, slot.Version);

            if (notifyNow)
            {
                Invoke_Internal(invoke, callback, state, arg, _value);
            }

            return subscription;
        }

        private void Grow_Internal()
        {
            var grown = SlotPool.Rent(_slotCount);

            if (_extraSlots.Length > 0)
            {
                Array.Copy(_extraSlots, grown, _extraSlots.Length);

                SlotPool.Return(_extraSlots);
            }

            _extraSlots = grown;
        }

        // Once nobody is bound, the extra slots go back to the pool for the next ViewModel.
        private void ReleaseIfUnused_Internal()
        {
            if (SubscriberCount > 0 || _notifyDepth > 0 || _slotCount == 0)
            {
                return;
            }

            if (_extraSlots.Length > 0)
            {
                SlotPool.Return(_extraSlots);

                _extraSlots = Array.Empty<Slot>();
            }

            _firstSlot = default;

            _slotCount = 0;

            _freeSlot = -1;
        }

        private ref Slot SlotAt_Internal(int index)
        {
            if (index == 0)
            {
                return ref _firstSlot;
            }

            return ref _extraSlots[index - 1];
        }

        private static void Invoke_Internal(Invoker invoke, Delegate callback, object state, object arg, T value)
        {
            try
            {
                invoke(callback, state, arg, value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

#endregion
    }
}
