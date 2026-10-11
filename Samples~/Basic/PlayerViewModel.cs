using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples
{
    // The ViewModel knows nothing about the UI: it only exposes observable properties and commands.
    public sealed class PlayerViewModel
    {
        private readonly Observable<float> _health = new(1f);

        public PlayerViewModel(string nickname)
        {
            Nickname = new Observable<string>(nickname);

            Respawn = new Command(OnRespawn, canExecute: false);
        }

        public Observable<string> Nickname { get; }

        // Read-only: widgets can display it but never write it.
        public IReadOnlyObservable<float> Health => _health;

        public Observable<int> Score { get; } = new();

        // Writable: a Slider bound to it is two-way.
        public Observable<float> Volume { get; } = new(0.8f);

        public Observable<bool> Muted { get; } = new();

        public Command Respawn { get; }

        public bool IsDead => _health.Value <= 0f;

        public void TakeDamage(float amount)
        {
            _health.Value = Mathf.Max(0f, _health.Value - amount);

            Respawn.CanExecute.Value = IsDead;
        }

        private void OnRespawn()
        {
            _health.Value = 1f;

            Score.Value = 0;

            Respawn.CanExecute.Value = false;
        }
    }
}
