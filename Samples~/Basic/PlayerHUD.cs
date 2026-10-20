using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Samples
{
    // The View (≈ UUserWidget). Most bindings live in the Inspector (View Bindings): nickname, score, health bar,
    // volume slider and mute toggle (two-way), Hit/Respawn buttons, death screen and "mute disables the slider".
    // Only what needs a calculation stays in code.
    public sealed class PlayerHUD : Widget<PlayerViewModel>
    {
        [SerializeField, BindWidget] private Image healthBar;

        [SerializeField, BindWidget] private TMP_Text healthLabel;

        [SerializeField, BindWidget] private TMP_Text volumeLabel;

        [SerializeField] private Color healthyColor = new(0.3f, 0.85f, 0.45f);

        [SerializeField] private Color criticalColor = new(0.95f, 0.3f, 0.3f);

        protected override void OnBind(PlayerViewModel viewModel)
        {
            // Custom bindings: a static lambda plus the state it needs, so nothing is captured or allocated.
            Bind.To(viewModel.Health, this, static (self, health) => self.OnHealthChanged(health));

            Bind.To(viewModel.Volume, volumeLabel, static (label, value) => label.SetText("{0:0}%", value * 100f));
        }

        private void OnHealthChanged(float health)
        {
            healthBar.color = health <= 0.25f ? criticalColor : healthyColor;

            healthLabel.SetText("{0:0}%", health * 100f);
        }
    }
}
