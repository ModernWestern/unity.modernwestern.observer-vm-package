using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples
{
    // Gameplay side: owns the ViewModel, hands it to the HUD and updates it every frame without allocating.
    public sealed class PlayerSimulator : MonoBehaviour
    {
        [SerializeField] private PlayerHUD hud;

        [SerializeField] private string nickname = "Player";

        [SerializeField] private float damagePerSecond = 0.1f;

        private PlayerViewModel _player;

        private float _scoreTimer;

        private void Awake()
        {
            _player = new PlayerViewModel(nickname);

            hud.SetViewModel(_player);
        }

        private void Update()
        {
            if (_player.IsDead)
            {
                return;
            }

            _player.TakeDamage(damagePerSecond * Time.deltaTime);

            _scoreTimer += Time.deltaTime;

            if (_scoreTimer >= 1f)
            {
                _scoreTimer -= 1f;

                _player.Score.Value++;
            }
        }
    }
}
