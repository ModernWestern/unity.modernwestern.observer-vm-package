using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // Stats of one character (the player or an enemy). The world changes them; nameplates and the HUD only show them.
    public sealed class CharacterViewModel
    {
        private readonly Observable<int> _health;

        private readonly Observable<int> _mana;

        private readonly Observable<float> _healthPercent = new(1f);

        private readonly Observable<float> _manaPercent = new(1f);

        private readonly Observable<bool> _isDead = new();

        private readonly Observable<int> _lastDamage = new();

        public CharacterViewModel(string name, int maxHealth, int maxMana)
        {
            Name = new Observable<string>(name);

            MaxHealth = maxHealth;

            MaxMana = maxMana;

            _health = new Observable<int>(maxHealth);

            _mana = new Observable<int>(maxMana);
        }

        public Observable<string> Name { get; }

        public int MaxHealth { get; }

        public int MaxMana { get; }

        // Read-only for widgets: only the world changes them, through the methods below.
        public IReadOnlyObservable<int> Health => _health;

        public IReadOnlyObservable<float> HealthPercent => _healthPercent;

        public IReadOnlyObservable<int> Mana => _mana;

        public IReadOnlyObservable<float> ManaPercent => _manaPercent;

        public IReadOnlyObservable<bool> IsDead => _isDead;

        // Raised on every hit, even when two hits deal the same damage: the floating damage numbers follow it.
        public IReadOnlyObservable<int> LastDamage => _lastDamage;

        public void TakeDamage(int amount)
        {
            if (_isDead.Value)
            {
                return;
            }

            SetHealth(_health.Value - amount);

            _lastDamage.SetValueWithoutNotify(amount);

            _lastDamage.Notify();
        }

        public void Heal(int amount)
        {
            if (!_isDead.Value)
            {
                SetHealth(_health.Value + amount);
            }
        }

        public bool TrySpendMana(int amount)
        {
            if (_mana.Value < amount)
            {
                return false;
            }

            SetMana(_mana.Value - amount);

            return true;
        }

        public void RestoreMana(int amount)
        {
            SetMana(_mana.Value + amount);
        }

        public void Revive()
        {
            SetHealth(MaxHealth);

            SetMana(MaxMana);
        }

        private void SetHealth(int value)
        {
            _health.Value = Mathf.Clamp(value, 0, MaxHealth);

            _healthPercent.Value = (float)_health.Value / MaxHealth;

            _isDead.Value = _health.Value == 0;
        }

        private void SetMana(int value)
        {
            _mana.Value = Mathf.Clamp(value, 0, MaxMana);

            _manaPercent.Value = MaxMana > 0 ? (float)_mana.Value / MaxMana : 0f;
        }
    }
}
