using System;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // The card a world object shows when the player comes close: a title, a text and one action.
    public sealed class InteractableViewModel
    {
        public InteractableViewModel(string title, string description, string actionLabel, Action interact)
        {
            Title = new Observable<string>(title);

            Description = new Observable<string>(description);

            ActionLabel = new Observable<string>(actionLabel);

            Interact = new Command(interact);
        }

        public Observable<string> Title { get; }

        public Observable<string> Description { get; }

        // Text of the button, e.g. "Open (E)".
        public Observable<string> ActionLabel { get; }

        public Observable<bool> IsInRange { get; } = new();

        // Runs from the E key and from the card's button. While it cannot run, the button greys out by itself.
        public Command Interact { get; }
    }
}
