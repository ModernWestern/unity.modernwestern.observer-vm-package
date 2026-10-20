using System;
using UnityEngine;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// The subscribers of an observable source (a value or a list), stored without allocating per change.
    /// Each subscriber is "state + static callback"; the first one lives inline and extra ones in arrays pooled per <typeparamref name="TArg"/>.
    /// </summary>
    internal sealed class SubscriberList<TArg> : ISubscriptionSource
    {
        private delegate void Invoker(Delegate callback, object state, object extra, TArg arg);

        private struct Slot
        {
            public object State;

            public object Extra;

            public Delegate Callback;

            public Invoker Invoke;

            public int Version;

            public int NextFree;
        }

        private static class Thunk<TState> where TState : class
        {
            public static readonly Invoker Invoke = (callback, state, _, arg) => ((Action<TState, TArg>)callback)((TState)state, arg);
        }

        private static class Thunk<TState, TExtra> where TState : class where TExtra : class
        {
            public static readonly Invoker Invoke = (callback, state, extra, arg) => ((Action<TState, TExtra, TArg>)callback)((TState)state, (TExtra)extra, arg);
        }

        // Arrays for sources with 2+ subscribers are shared per TArg: unbinding a ViewModel hands them to the next one.
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

        private Slot _firstSlot;

        private Slot[] _extraSlots = Array.Empty<Slot>();

        private int _freeSlot = -1;

        private int _lastVersion;

        private int _notifyDepth;

        /// <summary>
        /// Active subscribers.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Slots to visit when notifying (active and free ones).
        /// </summary>
        public int SlotCount { get; private set; }

        public Subscription Add<TState>(TState state, Action<TState, TArg> callback) where TState : class
        {
            return Add_Internal(state, null, callback, Thunk<TState>.Invoke);
        }

        public Subscription Add<TState, TExtra>(TState state, TExtra extra, Action<TState, TExtra, TArg> callback) where TState : class where TExtra : class
        {
            return Add_Internal(state, extra, callback, Thunk<TState, TExtra>.Invoke);
        }

        /// <summary>
        /// Calls one subscriber. Exceptions are logged so one broken binding does not stop the others.
        /// </summary>
        public void Invoke(int index, TArg arg)
        {
            var slot = SlotAt_Internal(index);

            if (slot.Callback == null)
            {
                return;
            }

            try
            {
                slot.Invoke(slot.Callback, slot.State, slot.Extra, arg);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Wrap every notification loop with Begin/End so freed slots are not recycled while being visited.
        /// </summary>
        public void BeginNotify()
        {
            _notifyDepth++;
        }

        public void EndNotify()
        {
            _notifyDepth--;

            ReleaseIfUnused_Internal();
        }

        /// <summary>
        /// Notifies every subscriber, in order.
        /// </summary>
        public void NotifyAll(TArg arg)
        {
            var count = SlotCount;

            BeginNotify();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    Invoke(i, arg);
                }
            }
            finally
            {
                EndNotify();
            }
        }

        void ISubscriptionSource.Unsubscribe(int index, int version)
        {
            if ((uint)index >= (uint)SlotCount)
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

            Count--;

            ReleaseIfUnused_Internal();
        }

        bool ISubscriptionSource.IsSubscribed(int index, int version)
        {
            if ((uint)index >= (uint)SlotCount)
            {
                return false;
            }

            ref var slot = ref SlotAt_Internal(index);

            return slot.Version == version && slot.Callback != null;
        }

#region Internal

        private Subscription Add_Internal(object state, object extra, Delegate callback, Invoker invoke)
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
                if (SlotCount > _extraSlots.Length)
                {
                    Grow_Internal();
                }

                index = SlotCount++;
            }

            ref var slot = ref SlotAt_Internal(index);

            slot.State = state;

            slot.Extra = extra;

            slot.Callback = callback;

            slot.Invoke = invoke;

            slot.NextFree = -1;

            // Versions are unique per source, so a stale Subscription can never remove whoever reuses its slot.
            slot.Version = ++_lastVersion;

            Count++;

            return new Subscription(this, index, slot.Version);
        }

        private void Grow_Internal()
        {
            var grown = SlotPool.Rent(SlotCount);

            if (_extraSlots.Length > 0)
            {
                Array.Copy(_extraSlots, grown, _extraSlots.Length);

                SlotPool.Return(_extraSlots);
            }

            _extraSlots = grown;
        }

        // Once nobody is subscribed, the extra slots go back to the pool for the next source.
        private void ReleaseIfUnused_Internal()
        {
            if (Count > 0 || _notifyDepth > 0 || SlotCount == 0)
            {
                return;
            }

            if (_extraSlots.Length > 0)
            {
                SlotPool.Return(_extraSlots);

                _extraSlots = Array.Empty<Slot>();
            }

            _firstSlot = default;

            SlotCount = 0;

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

#endregion
    }
}
