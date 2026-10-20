using TMPro;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // The HUD. It shows the player's stats with the same CharacterWidget used for the nameplates, and what the world
    // changes: gold, chests, enemies defeated and the last message. Gold, defeated and the message text are View Bindings.
    public sealed class HudWidget : Widget<GameViewModel>
    {
        [SerializeField, BindWidget] private CharacterWidget player;

        [SerializeField, BindWidget] private TMP_Text chests;

        [SerializeField, BindWidget, Tooltip("Faded out a few seconds after each message.")]
        private CanvasGroup message;

        [SerializeField] private float messageSeconds = 2.5f;

        [SerializeField] private float fadeSeconds = 0.5f;

        private float _messageTime = -1f;

        protected override void OnBind(GameViewModel game)
        {
            // The player's stats are a child ViewModel: the same object the player's nameplate is bound to.
            Bind.Child(player, game.Player);

            Bind.To(game.ChestsOpened, this, static (self, opened) => self.chests.SetText("{0} / {1}", opened, self.ViewModel.ChestCount));

            Bind.To(game.Message, this, static (self, text) => self.ShowMessage(text));
        }

        private void ShowMessage(string text)
        {
            var hasText = !string.IsNullOrEmpty(text);

            message.alpha = hasText ? 1f : 0f;

            _messageTime = hasText ? 0f : -1f;
        }

        private void Update()
        {
            if (_messageTime < 0f)
            {
                return;
            }

            _messageTime += Time.deltaTime;

            var fade = (_messageTime - messageSeconds) / fadeSeconds;

            message.alpha = 1f - Mathf.Clamp01(fade);

            if (fade >= 1f)
            {
                _messageTime = -1f;
            }
        }
    }
}
