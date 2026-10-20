namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // The card that pops up over a world object when the player comes close. It has no code: everything is a View Binding
    // (see the Inspector). The card follows IsInRange, the texts follow the ViewModel and the button runs Interact and
    // greys out by itself while the object cannot be used.
    public sealed class InteractionWidget : Widget<InteractableViewModel>
    {
    }
}
