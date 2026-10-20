using System;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// Optional base class for ViewModels that react to other ViewModels (derived state).
    /// <see cref="Watch{TState,T}(IReadOnlyObservable{T},TState,Action{TState,T},bool)"/> subscribes and remembers the subscription,
    /// and <see cref="Dispose"/> removes them all, so a ViewModel that is thrown away stops listening.
    /// </summary>
    public abstract class ViewModel : IDisposable
    {
        private BindingScope _watches;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            _watches?.Dispose();

            OnDispose();
        }

        /// <summary>
        /// Calls <paramref name="onChanged"/> whenever <paramref name="source"/> changes, until this ViewModel is disposed.
        /// Use a <c>static</c> lambda: <c>Watch(wallet.Gold, this, static (self, gold) => self.Buy.CanExecute.Value = gold >= self.Price);</c>
        /// </summary>
        protected void Watch<TState, T>(IReadOnlyObservable<T> source, TState state, Action<TState, T> onChanged, bool notifyNow = true) where TState : class
        {
            Watches_Internal().Add(source.Subscribe(state, onChanged, notifyNow));
        }

        /// <summary>
        /// Calls <paramref name="onChanged"/> after every change of <paramref name="source"/>, until this ViewModel is disposed.
        /// </summary>
        protected void Watch<TState, T>(IReadOnlyObservableList<T> source, TState state, Action<TState, ListChange<T>> onChanged) where TState : class
        {
            Watches_Internal().Add(source.Subscribe(state, onChanged));
        }

        /// <summary>
        /// Release what this ViewModel owns (e.g. dispose child ViewModels).
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        private BindingScope Watches_Internal()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return _watches ??= new BindingScope();
        }
    }
}
