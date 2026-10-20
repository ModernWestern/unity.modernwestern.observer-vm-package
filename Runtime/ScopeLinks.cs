using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    // Long-lived pieces of a BindingScope (UnityEvent listeners, child widgets, list rows). They are created once per widget
    // and re-targeted on every re-bind, because creating them allocates (e.g. UnityEvent.AddListener, Instantiate).
    internal abstract class ScopeLink
    {
        // What the link is attached to (a UnityEvent, a child widget, a row template): used to find it again on the next bind.
        public abstract object Key { get; }

        // Stops working but keeps its resources so the next bind can reuse them.
        public abstract void Detach();

        // Frees its resources (removes listeners, destroys pooled rows).
        public abstract void Release();
    }

    // UnityEvent<T> (View) → Observable<T> (ViewModel).
    internal sealed class ValueLink<T> : ScopeLink
    {
        private readonly UnityAction<T> _handler;

        private UnityEvent<T> _source;

        private Observable<T> _target;

        public ValueLink()
        {
            _handler = OnViewChanged_Internal;
        }

        public override object Key => _source;

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

    // UnityEvent (View) → Command (ViewModel).
    internal sealed class CommandLink : ScopeLink
    {
        private readonly UnityAction _handler;

        private UnityEvent _source;

        private Command _target;

        public CommandLink()
        {
            _handler = OnViewInvoked_Internal;
        }

        public override object Key => _source;

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

    // UnityEvent (View) → Command<T> (ViewModel) with a fixed parameter.
    internal sealed class CommandLink<T> : ScopeLink
    {
        private readonly UnityAction _handler;

        private UnityEvent _source;

        private Command<T> _target;

        private T _parameter;

        public CommandLink()
        {
            _handler = OnViewInvoked_Internal;
        }

        public override object Key => _source;

        public void Attach(UnityEvent source, Command<T> target, T parameter)
        {
            if (_source != source)
            {
                _source?.RemoveListener(_handler);

                _source = source;

                _source.AddListener(_handler);
            }

            _target = target;

            _parameter = parameter;
        }

        public override void Detach()
        {
            _target = null;

            _parameter = default;
        }

        public override void Release()
        {
            _source?.RemoveListener(_handler);

            _source = null;

            Detach();
        }

        private void OnViewInvoked_Internal()
        {
            _target?.Execute(_parameter);
        }
    }

    // A child widget that gets its ViewModel from the parent's bind, like a nested UUserWidget.
    internal sealed class ChildLink<TViewModel> : ScopeLink where TViewModel : class
    {
        private Widget<TViewModel> _child;

        private Subscription _subscription;

        public override object Key => _child;

        public void Attach(Widget<TViewModel> child, TViewModel viewModel)
        {
            _subscription.Dispose();

            _child = child;

            child.SetViewModel(viewModel);
        }

        public void Attach(Widget<TViewModel> child, IReadOnlyObservable<TViewModel> source)
        {
            _subscription.Dispose();

            _child = child;

            _subscription = source.Subscribe(this, static (link, viewModel) => link._child.SetViewModel(viewModel));
        }

        public override void Detach()
        {
            _subscription.Dispose();

            _subscription = default;

            if (_child != null)
            {
                _child.SetViewModel(null);
            }
        }

        public override void Release()
        {
            Detach();

            _child = null;
        }
    }

    // One row widget per list item, created from a template and pooled, like UMG's ListView entries.
    internal sealed class ListLink<TItem, TWidget> : ScopeLink where TItem : class where TWidget : Widget<TItem>
    {
        private readonly List<TWidget> _rows = new();

        private readonly Stack<TWidget> _pool = new();

        private TWidget _template;

        private IReadOnlyObservableList<TItem> _source;

        private Subscription _subscription;

        private int _created;

        public override object Key => _template;

        public void Attach(TWidget template, IReadOnlyObservableList<TItem> source)
        {
            _subscription.Dispose();

            _template = template;

            _template.gameObject.SetActive(false);

            _source = source;

            Rebuild_Internal();

            _subscription = source.Subscribe(this, static (link, change) => link.OnChanged_Internal(change));
        }

        public override void Detach()
        {
            _subscription.Dispose();

            _subscription = default;

            _source = null;

            ClearRows_Internal();
        }

        public override void Release()
        {
            Detach();

            while (_pool.Count > 0)
            {
                var row = _pool.Pop();

                if (row == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(row.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(row.gameObject);
                }
            }
        }

#region Internal

        private void OnChanged_Internal(ListChange<TItem> change)
        {
            switch (change.Kind)
            {
                case ListChangeKind.Add:
                {
                    var row = Rent_Internal();

                    _rows.Insert(change.Index, row);

                    row.SetViewModel(change.Item);

                    Reorder_Internal(change.Index);

                    break;
                }
                case ListChangeKind.Remove:
                {
                    var row = _rows[change.Index];

                    _rows.RemoveAt(change.Index);

                    Return_Internal(row);

                    Reorder_Internal(change.Index);

                    break;
                }
                case ListChangeKind.Replace:
                    _rows[change.Index].SetViewModel(change.Item);
                    break;
                case ListChangeKind.Move:
                {
                    var row = _rows[change.OldIndex];

                    _rows.RemoveAt(change.OldIndex);

                    _rows.Insert(change.Index, row);

                    Reorder_Internal(Math.Min(change.Index, change.OldIndex));

                    break;
                }
                default:
                    Rebuild_Internal();
                    break;
            }
        }

        private void Rebuild_Internal()
        {
            ClearRows_Internal();

            for (var i = 0; i < _source.Count; i++)
            {
                var row = Rent_Internal();

                _rows.Add(row);

                row.SetViewModel(_source[i]);
            }

            Reorder_Internal(0);
        }

        private void ClearRows_Internal()
        {
            for (var i = _rows.Count - 1; i >= 0; i--)
            {
                Return_Internal(_rows[i]);
            }

            _rows.Clear();
        }

        private TWidget Rent_Internal()
        {
            TWidget row;

            if (_pool.Count > 0)
            {
                row = _pool.Pop();
            }
            else
            {
                row = UnityEngine.Object.Instantiate(_template, _template.transform.parent);

                row.name = $"{RowName_Internal()} {++_created}";
            }

            row.gameObject.SetActive(true);

            return row;
        }

        private void Return_Internal(TWidget row)
        {
            if (row == null)
            {
                return;
            }

            row.SetViewModel(null);

            row.gameObject.SetActive(false);

            _pool.Push(row);
        }

        // "Row Template" → "Row 1", "Row 2"... (only computed when a row is created).
        private string RowName_Internal()
        {
            const string suffix = " Template";

            var name = _template.name;

            return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - suffix.Length) : name;
        }

        // Active rows sit right after the template, in list order (pooled, inactive rows end up after them).
        private void Reorder_Internal(int from)
        {
            var first = _template.transform.GetSiblingIndex() + 1;

            for (var i = from; i < _rows.Count; i++)
            {
                _rows[i].transform.SetSiblingIndex(first + i);
            }
        }

#endregion
    }
}
