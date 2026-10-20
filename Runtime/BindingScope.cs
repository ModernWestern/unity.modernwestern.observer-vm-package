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

        // [0, _linkCursor) are attached in the current bind; [_linkCursor, _linkCount) are detached but kept for the next bind.
        private ScopeLink[] _links = Array.Empty<ScopeLink>();

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
        /// View → ViewModel: executes <paramref name="command"/> with <paramref name="parameter"/> every time <paramref name="viewEvent"/> is raised.
        /// </summary>
        public void Event<T>(UnityEvent viewEvent, Command<T> command, T parameter)
        {
            var link = Claim_Internal<CommandLink<T>>(viewEvent) ?? Push_Internal(new CommandLink<T>());

            link.Attach(viewEvent, command, parameter);
        }

        /// <summary>
        /// Gives a child widget its ViewModel for as long as this scope is bound, like a nested UUserWidget.
        /// <c>Bind.Child(walletWidget, viewModel.Wallet);</c>
        /// </summary>
        public void Child<TViewModel>(Widget<TViewModel> child, TViewModel viewModel) where TViewModel : class
        {
            var link = Claim_Internal<ChildLink<TViewModel>>(child) ?? Push_Internal(new ChildLink<TViewModel>());

            link.Attach(child, viewModel);
        }

        /// <summary>
        /// Gives a child widget the ViewModel held by <paramref name="viewModel"/>, following it when it changes (e.g. the selected item).
        /// </summary>
        public void Child<TViewModel>(Widget<TViewModel> child, IReadOnlyObservable<TViewModel> viewModel) where TViewModel : class
        {
            var link = Claim_Internal<ChildLink<TViewModel>>(child) ?? Push_Internal(new ChildLink<TViewModel>());

            link.Attach(child, viewModel);
        }

        /// <summary>
        /// One row per item, like UMG's ListView: <paramref name="template"/> is hidden and copied for each item of <paramref name="items"/>,
        /// in its same parent. Rows follow every change of the list and are pooled, so adding, removing and re-binding do not allocate
        /// once enough rows exist. Keep the template alone in its container (e.g. under a LayoutGroup).
        /// </summary>
        public void List<TItem, TWidget>(TWidget template, IReadOnlyObservableList<TItem> items) where TItem : class where TWidget : Widget<TItem>
        {
            var link = Claim_Internal<ListLink<TItem, TWidget>>(template) ?? Push_Internal(new ListLink<TItem, TWidget>());

            link.Attach(template, items);
        }

        /// <summary>
        /// Unbinds everything. Keeps the memory, UnityEvent listeners and pooled rows for the next bind.
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
        /// Unbinds everything, removes the UnityEvent listeners and destroys pooled rows.
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
        private TLink Claim_Internal<TLink>(object key) where TLink : ScopeLink
        {
            for (var i = _linkCursor; i < _linkCount; i++)
            {
                if (_links[i] is not TLink link || !ReferenceEquals(link.Key, key))
                {
                    continue;
                }

                (_links[i], _links[_linkCursor]) = (_links[_linkCursor], _links[i]);

                _linkCursor++;

                return link;
            }

            return null;
        }

        private TLink Push_Internal<TLink>(TLink link) where TLink : ScopeLink
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
