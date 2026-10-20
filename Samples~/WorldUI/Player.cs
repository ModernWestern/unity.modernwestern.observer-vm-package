using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // The player cube: moves with WASD or the arrow keys, attacks with Space and uses the closest object with E.
    public sealed class Player : Character
    {
        [SerializeField] private float speed = 5f;

        [SerializeField] private float attackRadius = 2.5f;

        [SerializeField] private int attackDamage = 20;

        [SerializeField] private int attackMana = 10;

        [SerializeField, Tooltip("The player cannot leave this area (centered on the world origin).")]
        private Vector2 arenaSize = new(16f, 10f);

        [SerializeField, Tooltip("How close the player can get to an object before being pushed back.")]
        private float objectClearance = 1f;

        private Enemy[] _enemies;

        private Interactable[] _interactables;

        private Interactable _closest;

        public void SetWorld(Enemy[] enemies, Interactable[] interactables)
        {
            _enemies = enemies;

            _interactables = interactables;
        }

        // Public so they can also be called from UI buttons or tests.
        public void Attack()
        {
            if (Stats.IsDead.Value)
            {
                return;
            }

            if (!Stats.TrySpendMana(attackMana))
            {
                Game.ShowMessage("Not enough mana");

                return;
            }

            foreach (var enemy in _enemies)
            {
                if (!enemy.Stats.IsDead.Value && FlatDistance(enemy.transform.position) <= attackRadius)
                {
                    enemy.Stats.TakeDamage(attackDamage);
                }
            }
        }

        public void Interact()
        {
            // Same Command as the card's button: if it cannot run now (an empty chest), nothing happens.
            if (_closest != null)
            {
                _closest.Prompt.Interact.Execute();
            }
        }

        protected override void Act()
        {
            Move(ReadMove());

            UpdateClosest();

            if (AttackPressed())
            {
                Attack();
            }

            if (InteractPressed())
            {
                Interact();
            }
        }

        protected override void OnDefeated()
        {
            _closest = null;

            foreach (var interactable in _interactables)
            {
                interactable.Prompt.IsInRange.Value = false;
            }

            Game.ShowMessage("You were defeated. Back in a moment...");
        }

        private void Move(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

            var direction = new Vector3(input.x, 0f, input.y).normalized;

            var position = transform.position + direction * (speed * Time.deltaTime);

            // Walk around objects instead of through them.
            foreach (var interactable in _interactables)
            {
                var away = position - interactable.transform.position;

                away.y = 0f;

                if (away.sqrMagnitude < objectClearance * objectClearance && away.sqrMagnitude > 0.0001f)
                {
                    var pushed = interactable.transform.position + away.normalized * objectClearance;

                    position = new Vector3(pushed.x, position.y, pushed.z);
                }
            }

            position.x = Mathf.Clamp(position.x, -arenaSize.x * 0.5f, arenaSize.x * 0.5f);

            position.z = Mathf.Clamp(position.z, -arenaSize.y * 0.5f, arenaSize.y * 0.5f);

            transform.position = position;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Shows the card of every object in range and remembers the closest one for the E key.
        private void UpdateClosest()
        {
            _closest = null;

            var closestDistance = float.MaxValue;

            foreach (var interactable in _interactables)
            {
                var distance = FlatDistance(interactable.transform.position);

                var inRange = distance <= interactable.Radius;

                // Observable only notifies when the value changes, so setting it every frame is free.
                interactable.Prompt.IsInRange.Value = inRange;

                if (inRange && distance < closestDistance)
                {
                    closestDistance = distance;

                    _closest = interactable;
                }
            }
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static Vector2 ReadMove()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var move = Vector2.zero;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;

            return move;
        }

        private static bool AttackPressed() => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        private static bool InteractPressed() => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        private static Vector2 ReadMove() => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        private static bool AttackPressed() => Input.GetKeyDown(KeyCode.Space);

        private static bool InteractPressed() => Input.GetKeyDown(KeyCode.E);
#endif
    }
}
