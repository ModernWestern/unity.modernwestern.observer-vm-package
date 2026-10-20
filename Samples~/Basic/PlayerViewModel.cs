using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples
{
    // The ViewModel knows nothing about the UI: it only exposes observable properties and commands.
    public sealed class PlayerViewModel
    {
        private const float HitDamage = 0.25f;

        private readonly Observable<float> _health = new(1f);

        private readonly Observable<bool> _isDead = new(false);

        public PlayerViewModel(string nickname)
        {
            Nickname = new Observable<string>(nickname);

            Hit = new Command(OnHit);

            Respawn = new Command(OnRespawn, canExecute: false);
        }

        public Observable<string> Nickname { get; }

        // Read-only: widgets can display it but never write it.
        public IReadOnlyObservable<float> Health => _health;

        public IReadOnlyObservable<bool> IsDead => _isDead;

        public Observable<int> Score { get; } = new();

        // Writable: a Slider bound to it is two-way.
        public Observable<float> Volume { get; } = new(0.8f);

        public Observable<bool> Muted { get; } = new();

        public Command Hit { get; }

        public Command Respawn { get; }

        public void TakeDamage(float amount)
        {
            _health.Value = Mathf.Max(0f, _health.Value - amount);

            _isDead.Value = _health.Value <= 0f;

            Hit.CanExecute.Value = !_isDead.Value;

            Respawn.CanExecute.Value = _isDead.Value;
        }

        private void OnHit()
        {
            TakeDamage(HitDamage);
        }

        private void OnRespawn()
        {
            _health.Value = 1f;

            _isDead.Value = false;

            Score.Value = 0;

            Hit.CanExecute.Value = true;

            Respawn.CanExecute.Value = false;
        }
    }
}
