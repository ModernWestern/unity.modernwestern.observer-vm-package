using UnityEngine.Events;

namespace ModernWestern.UI.ObserverVM
{
    // Bridges a widget's UnityEvent to the ViewModel (View → ViewModel).
    // UnityEvent.AddListener/RemoveListener allocate, so a link registers its listener once and is re-targeted on every re-bind.
    internal abstract class EventLink
    {
        public abstract UnityEventBase Source { get; }

        // Stops forwarding but keeps the listener registered so the next bind can reuse it.
        public abstract void Detach();

        // Removes the listener from the UnityEvent.
        public abstract void Release();
    }

    internal sealed class ValueLink<T> : EventLink
    {
        private readonly UnityAction<T> _handler;

        private UnityEvent<T> _source;

        private Observable<T> _target;

        public ValueLink()
        {
            _handler = OnViewChanged_Internal;
        }

        public override UnityEventBase Source => _source;

        public void Attach(UnityEvent<T> source, Observable<T> target)
        {
            if (_source != source)
            {
                _source?.RemoveListener(_handler);

                _source = source;

                _source.AddListener(_handler);
            }

            _target = target;
        }

        public override void Detach()
        {
            _target = null;
        }

        public override void Release()
        {
            _source?.RemoveListener(_handler);

            _source = null;

            _target = null;
        }

        private void OnViewChanged_Internal(T value)
        {
            if (_target != null)
            {
                _target.Value = value;
            }
        }
    }

    internal sealed class CommandLink : EventLink
    {
        private readonly UnityAction _handler;

        private UnityEvent _source;

        private Command _target;

        public CommandLink()
        {
            _handler = OnViewInvoked_Internal;
        }

        public override UnityEventBase Source => _source;

        public void Attach(UnityEvent source, Command target)
        {
            if (_source != source)
            {
                _source?.RemoveListener(_handler);

                _source = source;

                _source.AddListener(_handler);
            }

            _target = target;
        }

        public override void Detach()
        {
            _target = null;
        }

        public override void Release()
        {
            _source?.RemoveListener(_handler);

            _source = null;

            _target = null;
        }

        private void OnViewInvoked_Internal()
        {
            _target?.Execute();
        }
    }
}
