using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples
{
    // Gameplay side: owns the ViewModel, hands it to the HUD and updates it every frame without allocating.
    public sealed class PlayerSimulator : MonoBehaviour
    {
        [SerializeField] private PlayerHUD hud;

        [SerializeField] private string nickname = "Player";

        [SerializeField, Tooltip("Damage taken every second while alive. 0 = only the Hit button hurts.")]
        private float damagePerSecond = 0.02f;

        private PlayerViewModel _player;

        private float _scoreTimer;

        private void Awake()
        {
            _player = new PlayerViewModel(nickname);

            hud.SetViewModel(_player);
        }

        private void Update()
        {
            if (_player.IsDead.Value)
            {
                return;
            }

            if (damagePerSecond > 0f)
            {
                _player.TakeDamage(damagePerSecond * Time.deltaTime);
            }

            _scoreTimer += Time.deltaTime;

            if (_scoreTimer >= 1f)
            {
                _scoreTimer -= 1f;

                _player.Score.Value++;
            }
        }
    }
}
