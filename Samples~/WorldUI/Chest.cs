using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // Gives gold once. After that the card says "Empty" and its button stays greyed out.
    public sealed class Chest : Interactable
    {
        [SerializeField] private int gold = 25;

        [SerializeField, Tooltip("Rotated open when the chest is used.")]
        private Transform lid;

        protected override InteractableViewModel CreatePrompt()
        {
            return new InteractableViewModel("Chest", "Something shines inside.", "Open (E)", Open);
        }

        private void Open()
        {
            Game.Gold.Value += gold;

            Game.ChestsOpened.Value++;

            Game.ShowMessage($"Found {gold} gold");

            Prompt.Description.Value = "Empty.";

            Prompt.Interact.CanExecute.Value = false;

            if (lid != null)
            {
                lid.localRotation = Quaternion.Euler(100f, 0f, 0f);
            }
        }
    }
}
