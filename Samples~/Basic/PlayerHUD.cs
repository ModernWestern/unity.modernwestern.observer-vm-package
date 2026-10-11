using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Samples
{
    // The View (≈ UUserWidget): references its widgets and declares the bindings. No game logic here.
    public sealed class PlayerHUD : Widget<PlayerViewModel>
    {
        [SerializeField, BindWidget] private TMP_Text nickname;

        [SerializeField, BindWidget] private TMP_Text score;

        [SerializeField, BindWidget] private Image healthBar;

        [SerializeField, BindWidget] private TMP_Text healthLabel;

        [SerializeField, BindWidget] private Slider volume;

        [SerializeField, BindWidget] private TMP_Text volumeLabel;

        [SerializeField, BindWidget] private Toggle muted;

        [SerializeField, BindWidget] private Button hit;

        [SerializeField, BindWidget] private Button respawn;

        [SerializeField, BindWidget(Optional = true)] private GameObject deathScreen;

        [SerializeField] private Color healthyColor = new(0.3f, 0.85f, 0.45f);

        [SerializeField] private Color criticalColor = new(0.95f, 0.3f, 0.3f);

        protected override void OnBind(PlayerViewModel viewModel)
        {
            Bind.Text(nickname, viewModel.Nickname);

            Bind.Text(score, viewModel.Score, "Score {0}");

            Bind.Fill(healthBar, viewModel.Health);

            Bind.Value(volume, viewModel.Volume);

            Bind.IsOn(muted, viewModel.Muted);

            Bind.Click(hit, viewModel.Hit);

            Bind.Click(respawn, viewModel.Respawn);

            if (deathScreen != null)
            {
                Bind.Active(deathScreen, viewModel.Respawn.CanExecute);
            }

            // Custom bindings: a static lambda plus the state it needs, so nothing is captured or allocated.
            Bind.To(viewModel.Health, this, static (self, health) => self.OnHealthChanged(health));

            Bind.To(viewModel.Volume, volumeLabel, static (label, value) => label.SetText("{0:0}%", value * 100f));

            Bind.To(viewModel.Muted, volume, static (slider, isMuted) => slider.interactable = !isMuted);
        }

        private void OnHealthChanged(float health)
        {
            healthBar.color = health <= 0.25f ? criticalColor : healthyColor;

            healthLabel.SetText("{0:0}%", health * 100f);
        }
    }
}
