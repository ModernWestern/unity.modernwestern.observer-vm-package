using System;

namespace ModernWestern.UI.ObserverVM
{
    internal interface ISubscriptionSource
    {
        void Unsubscribe(int index, int version);

        bool IsSubscribed(int index, int version);
    }

    /// <summary>
    /// Handle returned by <see cref="IReadOnlyObservable{T}.Subscribe{TState}"/>. It is a struct: disposing it does not allocate,
    /// and disposing it twice (or after its slot was reused) is a no-op.
    /// </summary>
    public readonly struct Subscription : IDisposable
    {
        private readonly ISubscriptionSource _source;

        private readonly int _index;

        private readonly int _version;

        internal Subscription(ISubscriptionSource source, int index, int version)
        {
            _source = source;

            _index = index;

            _version = version;
        }

        public bool IsActive => _source != null && _source.IsSubscribed(_index, _version);

        public void Dispose()
        {
            _source?.Unsubscribe(_index, _version);
        }
    }
}
