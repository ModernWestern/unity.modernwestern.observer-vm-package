using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // Base of the player and the enemies: owns the stats ViewModel, regenerates mana, hides the body on death and
    // respawns. Like everything in the world, it only changes ViewModels and never touches the UI.
    public abstract class Character : MonoBehaviour
    {
        [SerializeField] private string displayName = "Character";

        [SerializeField] private int maxHealth = 100;

        [SerializeField] private int maxMana = 50;

        [SerializeField, Tooltip("Mana recovered every second.")]
        private int manaPerSecond = 5;

        [SerializeField] private float respawnSeconds = 5f;

        [SerializeField, Tooltip("Hidden while the character is dead.")]
        private GameObject body;

        private Vector3 _spawnPoint;

        private float _manaTimer;

        private float _respawnTimer;

        public CharacterViewModel Stats { get; private set; }

        protected GameViewModel Game { get; private set; }

        public void Init(GameViewModel game)
        {
            Game = game;
        }

        protected virtual void Awake()
        {
            Stats = new CharacterViewModel(displayName, maxHealth, maxMana);

            _spawnPoint = transform.position;
        }

        protected virtual void Update()
        {
            if (Stats.IsDead.Value)
            {
                UpdateDead();

                return;
            }

            _manaTimer += Time.deltaTime;

            if (_manaTimer >= 1f)
            {
                _manaTimer -= 1f;

                Stats.RestoreMana(manaPerSecond);
            }

            Act();
        }

        // What the character does every frame while alive.
        protected abstract void Act();

        protected virtual void OnDefeated()
        {
        }

        protected float FlatDistance(Vector3 point)
        {
            var delta = point - transform.position;

            delta.y = 0f;

            return delta.magnitude;
        }

        protected void Face(Vector3 point)
        {
            var direction = point - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void UpdateDead()
        {
            if (body.activeSelf)
            {
                body.SetActive(false);

                _respawnTimer = respawnSeconds;

                OnDefeated();
            }

            _respawnTimer -= Time.deltaTime;

            if (_respawnTimer > 0f)
            {
                return;
            }

            transform.position = _spawnPoint;

            Stats.Revive();

            body.SetActive(true);
        }
    }
}
