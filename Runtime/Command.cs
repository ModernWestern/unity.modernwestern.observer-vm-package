using System;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// A ViewModel action a widget can trigger (e.g. a button). <see cref="CanExecute"/> drives the widget's interactable state.
    /// </summary>
    public sealed class Command
    {
        private readonly Action _execute;

        public Command(Action execute, bool canExecute = true)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));

            CanExecute = new Observable<bool>(canExecute);
        }

        public Observable<bool> CanExecute { get; }

        public void Execute()
        {
            if (CanExecute.Value)
            {
                _execute();
            }
        }
    }
}
