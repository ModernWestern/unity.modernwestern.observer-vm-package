using System;
using System.Collections;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    public enum ListChangeKind
    {
        Add,
        Remove,
        Replace,
        Move,
        Reset
    }

    /// <summary>
    /// What changed in an <see cref="ObservableList{T}"/>. A struct: notifying does not allocate.
    /// </summary>
    public readonly struct ListChange<T>
    {
        public ListChange(ListChangeKind kind, int index, int oldIndex, T item, T oldItem)
        {
            Kind = kind;

            Index = index;

            OldIndex = oldIndex;

            Item = item;

            OldItem = oldItem;
        }

        public ListChangeKind Kind { get; }

        /// <summary>
        /// Where the item is now (Add, Replace, Move) or was (Remove). -1 for Reset.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Where the item was before a Move. -1 otherwise.
        /// </summary>
        public int OldIndex { get; }

        public T Item { get; }

        /// <summary>
        /// The replaced item (Replace) or the removed item (Remove).
        /// </summary>
        public T OldItem { get; }
    }

    /// <summary>
    /// Read-only side of an <see cref="ObservableList{T}"/>, e.g. to bind it with <c>Bind.List</c>.
    /// </summary>
    public interface IReadOnlyObservableList<T> : IReadOnlyList<T>
    {
        int SubscriberCount { get; }

        /// <summary>
        /// Calls <paramref name="onChanged"/> with <paramref name="state"/> after every change. Use a <c>static</c> lambda.
        /// </summary>
        Subscription Subscribe<TState>(TState state, Action<TState, ListChange<T>> onChanged) where TState : class;
    }

    /// <summary>
    /// A list that notifies its bindings when items are added, removed, replaced or moved (ViewModel collections, like UMG's ListView items).
    /// Changes do not allocate beyond what <see cref="List{T}"/> needs to grow. Not thread-safe.
    /// </summary>
    public sealed class ObservableList<T> : IReadOnlyObservableList<T>
    {
        private readonly List<T> _items;

        private readonly SubscriberList<ListChange<T>> _subscribers = new();

        public ObservableList()
        {
            _items = new List<T>();
        }

        public ObservableList(int capacity)
        {
            _items = new List<T>(capacity);
        }

        public ObservableList(IEnumerable<T> items)
        {
            _items = new List<T>(items);
        }

        public int Count => _items.Count;

        public int SubscriberCount => _subscribers.Count;

        public T this[int index]
        {
            get => _items[index];
            set
            {
                var old = _items[index];

                _items[index] = value;

                _subscribers.NotifyAll(new ListChange<T>(ListChangeKind.Replace, index, -1, value, old));
            }
        }

        public void Add(T item)
        {
            Insert(_items.Count, item);
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);

            _subscribers.NotifyAll(new ListChange<T>(ListChangeKind.Add, index, -1, item, default));
        }

        public bool Remove(T item)
        {
            var index = _items.IndexOf(item);

            if (index < 0)
            {
                return false;
            }

            RemoveAt(index);

            return true;
        }

        public void RemoveAt(int index)
        {
            var old = _items[index];

            _items.RemoveAt(index);

            _subscribers.NotifyAll(new ListChange<T>(ListChangeKind.Remove, index, -1, default, old));
        }

        public void Move(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex)
            {
                return;
            }

            var item = _items[oldIndex];

            _items.RemoveAt(oldIndex);

            _items.Insert(newIndex, item);

            _subscribers.NotifyAll(new ListChange<T>(ListChangeKind.Move, newIndex, oldIndex, item, default));
        }

        public void Clear()
        {
            _items.Clear();

            _subscribers.NotifyAll(new ListChange<T>(ListChangeKind.Reset, -1, -1, default, default));
        }

        public int IndexOf(T item) => _items.IndexOf(item);

        public bool Contains(T item) => _items.Contains(item);

        public Subscription Subscribe<TState>(TState state, Action<TState, ListChange<T>> onChanged) where TState : class
        {
            return _subscribers.Add(state, onChanged);
        }

        /// <summary>
        /// Struct enumerator: <c>foreach</c> over the list does not allocate.
        /// </summary>
        public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
