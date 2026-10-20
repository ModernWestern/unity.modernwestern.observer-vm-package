using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // Shows a short text when you come close; "Read" unfolds the full text and it folds back when you walk away.
    public sealed class Sign : Interactable
    {
        [SerializeField] private string title = "Signpost";

        [SerializeField, TextArea] private string summary = "Tips for new adventurers.";

        [SerializeField, TextArea] private string details = "Space attacks the enemies around you (10 mana).\nE uses the closest object.\nDefeated enemies come back after 5 seconds.";

        private bool _open;

        protected override InteractableViewModel CreatePrompt()
        {
            return new InteractableViewModel(title, summary, "Read (E)", Toggle);
        }

        private void Toggle()
        {
            SetOpen(!_open);
        }

        private void Update()
        {
            if (_open && !Prompt.IsInRange.Value)
            {
                SetOpen(false);
            }
        }

        private void SetOpen(bool open)
        {
            _open = open;

            Prompt.Description.Value = open ? details : summary;

            Prompt.ActionLabel.Value = open ? "Close (E)" : "Read (E)";
        }
    }
}
