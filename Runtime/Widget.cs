using System;
using UnityEngine;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// Non-generic side of every <see cref="Widget{TViewModel}"/>, so tools and code can handle any widget the same way.
    /// Derive from <see cref="Widget{TViewModel}"/>, not from this class.
    /// </summary>
    public abstract class Widget : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        private ViewBinding[] viewBindings = Array.Empty<ViewBinding>();

        /// <summary>
        /// Bindings configured in the Inspector (View Bindings). They are applied right after <c>OnBind</c>.
        /// </summary>
        public IReadOnlyList<ViewBinding> ViewBindings => viewBindings;

        internal ViewBinding[] ViewBindingArray
        {
            get => viewBindings;
            set => viewBindings = value ?? Array.Empty<ViewBinding>();
        }

        /// <summary>
        /// The ViewModel currently bound, or null.
        /// </summary>
        public abstract object BoundViewModel { get; }

        /// <summary>
        /// The ViewModel type this widget expects.
        /// </summary>
        public abstract Type ViewModelType { get; }

        /// <summary>
        /// How many bindings are active right now.
        /// </summary>
        public abstract int BindingCount { get; }
    }

    /// <summary>
    /// Base class for a view, like Unreal's UUserWidget. Reference the UI with <c>[SerializeField, BindWidget]</c> fields,
    /// declare the bindings in <see cref="OnBind"/> and hand it a ViewModel with <see cref="SetViewModel"/>.
    /// Bindings are removed automatically when the ViewModel changes or the widget is destroyed.
    /// </summary>
    public abstract class Widget<TViewModel> : Widget where TViewModel : class
    {
        private BindingScope _bind;

        public TViewModel ViewModel { get; private set; }

        public override object BoundViewModel => ViewModel;

        public override Type ViewModelType => typeof(TViewModel);

        public override int BindingCount => _bind?.Count ?? 0;

        protected BindingScope Bind => _bind ??= new BindingScope();

        public void SetViewModel(TViewModel viewModel)
        {
            if (ReferenceEquals(ViewModel, viewModel))
            {
                return;
            }

            Unbind_Internal();

            ViewModel = viewModel;

            if (viewModel != null)
            {
                OnBind(viewModel);

                ViewBindingResolver<TViewModel>.Apply(Bind, viewModel, ViewBindingArray, this);
            }
        }

        /// <summary>
        /// Declare the bindings in code here, e.g. <c>Bind.Text(nickname, viewModel.Nickname);</c>
        /// Optional: bindings can also be configured in the Inspector (View Bindings), which run right after this.
        /// </summary>
        protected virtual void OnBind(TViewModel viewModel)
        {
        }

        /// <summary>
        /// Called before the bindings to <paramref name="viewModel"/> are removed.
        /// </summary>
        protected virtual void OnUnbind(TViewModel viewModel)
        {
        }

        protected virtual void OnDestroy()
        {
            Unbind_Internal();

            ViewModel = null;

            _bind?.Dispose();
        }

#region Internal

        private void Unbind_Internal()
        {
            if (ViewModel == null)
            {
                return;
            }

            OnUnbind(ViewModel);

            _bind?.Clear();
        }

#endregion
    }
}
