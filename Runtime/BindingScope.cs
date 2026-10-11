using System;
using UnityEngine.Events;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// Owns every binding of a widget. <see cref="Clear"/> unbinds them all; re-binding afterwards reuses the memory,
    /// so swapping ViewModels (e.g. pooled list items) does not allocate.
    /// Widget-specific helpers (<c>Bind.Text</c>, <c>Bind.Value</c>, <c>Bind.Click</c>...) are extension methods of this class.
    /// </summary>
    public sealed class BindingScope : IDisposable
    {
        private Subscription[] _subscriptions = Array.Empty<Subscription>();

        private int _subscriptionCount;

        // [0, _linkCursor) are attached in the current bind; [_linkCursor, _linkCount) are detached but still listening.
        private EventLink[] _links = Array.Empty<EventLink>();

        private int _linkCount;

        private int _linkCursor;

        public int Count => _subscriptionCount + _linkCursor;

        public void Add(Subscription subscription)
        {
            if (_subscriptionCount == _subscriptions.Length)
            {
                Array.Resize(ref _subscriptions, Math.Max(8, _subscriptions.Length * 2));
            }

            _subscriptions[_subscriptionCount++] = subscription;
        }

        /// <summary>
        /// One-way custom binding: calls <paramref name="apply"/> with <paramref name="state"/> whenever <paramref name="source"/> changes.
        /// Use a <c>static</c> lambda: <c>Bind.To(vm.Health, this, static (self, hp) => self.Flash(hp));</c>
        /// </summary>
        public void To<TState, T>(IReadOnlyObservable<T> source, TState state, Action<TState, T> apply) where TState : class
        {
            Add(source.Subscribe(state, apply));
        }

        /// <inheritdoc cref="To{TState,T}"/>
        public void To<TState, TArg, T>(IReadOnlyObservable<T> source, TState state, TArg arg, Action<TState, TArg, T> apply) where TState : class where TArg : class
        {
            Add(source.Subscribe(state, arg, apply));
        }

        /// <summary>
        /// View → ViewModel: writes every value raised by <paramref name="viewEvent"/> into <paramref name="target"/>.
        /// </summary>
        public void Event<T>(UnityEvent<T> viewEvent, Observable<T> target)
        {
            var link = Claim_Internal<ValueLink<T>>(viewEvent) ?? Push_Internal(new ValueLink<T>());

            link.Attach(viewEvent, target);
        }

        /// <summary>
        /// View → ViewModel: executes <paramref name="command"/> every time <paramref name="viewEvent"/> is raised.
        /// </summary>
        public void Event(UnityEvent viewEvent, Command command)
        {
            var link = Claim_Internal<CommandLink>(viewEvent) ?? Push_Internal(new CommandLink());

            link.Attach(viewEvent, command);
        }

        /// <summary>
        /// Unbinds everything. Keeps the memory and UnityEvent listeners for the next bind.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _subscriptionCount; i++)
            {
                _subscriptions[i].Dispose();

                _subscriptions[i] = default;
            }

            _subscriptionCount = 0;

            for (var i = 0; i < _linkCursor; i++)
            {
                _links[i].Detach();
            }

            _linkCursor = 0;
        }

        /// <summary>
        /// Unbinds everything and removes the UnityEvent listeners.
        /// </summary>
        public void Dispose()
        {
            Clear();

            for (var i = 0; i < _linkCount; i++)
            {
                _links[i].Release();

                _links[i] = null;
            }

            _linkCount = 0;
        }

#region Internal

        // Bindings are usually declared in the same order on every bind, so the link we need is normally the one at the cursor.
        private TLink Claim_Internal<TLink>(UnityEventBase source) where TLink : EventLink
        {
            for (var i = _linkCursor; i < _linkCount; i++)
            {
                if (_links[i] is not TLink link || !ReferenceEquals(link.Source, source))
                {
                    continue;
                }

                (_links[i], _links[_linkCursor]) = (_links[_linkCursor], _links[i]);

                _linkCursor++;

                return link;
            }

            return null;
        }

        private TLink Push_Internal<TLink>(TLink link) where TLink : EventLink
        {
            if (_linkCount == _links.Length)
            {
                Array.Resize(ref _links, Math.Max(4, _links.Length * 2));
            }

            _links[_linkCount++] = _links[_linkCursor];

            _links[_linkCursor++] = link;

            return link;
        }

#endregion
    }
}
