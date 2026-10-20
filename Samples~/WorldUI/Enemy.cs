using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // An enemy cube: patrols back and forth and hits the player when close. It does not know it has a nameplate.
    public sealed class Enemy : Character
    {
        [SerializeField, Tooltip("The enemy walks from its start position to start + this offset and back.")]
        private Vector3 patrol = new(4f, 0f, 0f);

        [SerializeField] private float patrolSeconds = 4f;

        [SerializeField] private float reach = 1.6f;

        [SerializeField] private float secondsBetweenHits = 1.2f;

        [SerializeField] private int damage = 6;

        [SerializeField, Tooltip("While it has mana, every hit costs mana and deals this instead.")]
        private int heavyDamage = 12;

        [SerializeField] private int heavyMana = 10;

        private Player _target;

        private Vector3 _origin;

        private float _patrolTime;

        private float _hitTimer;

        public void SetTarget(Player player)
        {
            _target = player;
        }

        protected override void Awake()
        {
            base.Awake();

            _origin = transform.position;
        }

        protected override void Act()
        {
            var previous = transform.position;

            _patrolTime += Time.deltaTime;

            transform.position = _origin + patrol * Mathf.SmoothStep(0f, 1f, Mathf.PingPong(_patrolTime / patrolSeconds, 1f));

            var targetAlive = !_target.Stats.IsDead.Value;

            var targetInReach = targetAlive && FlatDistance(_target.transform.position) <= reach;

            Face(targetInReach ? _target.transform.position : transform.position + (transform.position - previous));

            _hitTimer -= Time.deltaTime;

            if (!targetInReach || _hitTimer > 0f)
            {
                return;
            }

            _hitTimer = secondsBetweenHits;

            _target.Stats.TakeDamage(Stats.TrySpendMana(heavyMana) ? heavyDamage : damage);
        }

        protected override void OnDefeated()
        {
            Game.Defeated.Value++;

            Game.ShowMessage("Enemy defeated");
        }
    }
}
