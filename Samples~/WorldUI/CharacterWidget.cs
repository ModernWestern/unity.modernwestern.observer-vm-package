using TMPro;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // Stats of a character, used with two layouts: the nameplate over every cube and the big panel of the HUD.
    // The name, the bars and "hide when dead" are View Bindings (see the Inspector); the "75 / 100" labels and the
    // floating damage numbers need a calculation, so they are written here.
    public sealed class CharacterWidget : Widget<CharacterViewModel>
    {
        [SerializeField, BindWidget] private TMP_Text healthLabel;

        [SerializeField, BindWidget] private TMP_Text manaLabel;

        [SerializeField, BindWidget(Optional = true), Tooltip("Floating number shown on every hit.")]
        private TMP_Text damagePopup;

        [SerializeField] private float popupSeconds = 0.9f;

        [SerializeField] private float popupRise = 40f;

        private Vector2 _popupStart;

        private float _popupTime = -1f;

        private void Awake()
        {
            if (damagePopup != null)
            {
                _popupStart = damagePopup.rectTransform.anchoredPosition;

                damagePopup.gameObject.SetActive(false);
            }
        }

        protected override void OnBind(CharacterViewModel stats)
        {
            // Two numbers in one label. Static lambdas with "this" as state: nothing is captured or allocated.
            Bind.To(stats.Health, this, static (self, health) => self.healthLabel.SetText("{0} / {1}", health, self.ViewModel.MaxHealth));

            Bind.To(stats.Mana, this, static (self, mana) => self.manaLabel.SetText("{0} / {1}", mana, self.ViewModel.MaxMana));

            if (damagePopup != null)
            {
                Bind.To(stats.LastDamage, this, static (self, damage) => self.ShowDamage(damage));
            }
        }

        private void ShowDamage(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            damagePopup.SetText("-{0}", damage);

            damagePopup.gameObject.SetActive(true);

            _popupTime = 0f;
        }

        // A view-only animation: the ViewModel only says "you took 20 damage", the widget decides how it looks.
        private void Update()
        {
            if (_popupTime < 0f)
            {
                return;
            }

            _popupTime += Time.deltaTime;

            var progress = _popupTime / popupSeconds;

            if (progress >= 1f)
            {
                _popupTime = -1f;

                damagePopup.gameObject.SetActive(false);

                return;
            }

            damagePopup.rectTransform.anchoredPosition = _popupStart + new Vector2(0f, popupRise * progress);

            damagePopup.alpha = 1f - progress * progress;
        }
    }
}
