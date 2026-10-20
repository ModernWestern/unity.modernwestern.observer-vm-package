using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // A world object the player can use. It owns the ViewModel of its card and changes the game state when used.
    // It never touches the UI: the card and the HUD update by themselves.
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField, Tooltip("The card shows up when the player is this close.")]
        private float radius = 2.2f;

        public float Radius => radius;

        public InteractableViewModel Prompt { get; private set; }

        protected GameViewModel Game { get; private set; }

        public void Init(GameViewModel game)
        {
            Game = game;

            Prompt = CreatePrompt();
        }

        protected abstract InteractableViewModel CreatePrompt();
    }
}
