using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // Restores the player's health and mana, then needs a few seconds to recharge.
    public sealed class Shrine : Interactable
    {
        private const string ReadyText = "Restores your health and mana.";

        [SerializeField] private float rechargeSeconds = 8f;

        [SerializeField, Tooltip("Spins while the shrine is ready.")]
        private Transform crystal;

        private float _recharge;

        protected override InteractableViewModel CreatePrompt()
        {
            return new InteractableViewModel("Shrine", ReadyText, "Pray (E)", Pray);
        }

        private void Pray()
        {
            Game.Player.Heal(Game.Player.MaxHealth);

            Game.Player.RestoreMana(Game.Player.MaxMana);

            Game.ShowMessage("The shrine restores you");

            Prompt.Description.Value = "Recharging...";

            Prompt.Interact.CanExecute.Value = false;

            _recharge = rechargeSeconds;
        }

        private void Update()
        {
            if (_recharge <= 0f)
            {
                if (crystal != null)
                {
                    crystal.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
                }

                return;
            }

            _recharge -= Time.deltaTime;

            if (_recharge <= 0f)
            {
                Prompt.Description.Value = ReadyText;

                Prompt.Interact.CanExecute.Value = true;
            }
        }
    }
}
