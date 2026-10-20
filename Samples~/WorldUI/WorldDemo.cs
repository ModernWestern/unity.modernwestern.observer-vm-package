using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // The only script that knows both sides. It creates the game state, gives each world object what it needs and spawns
    // one nameplate per character and one card per object, each bound to that object's ViewModel.
    public sealed class WorldDemo : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private Player player;

        [SerializeField] private Enemy[] enemies;

        [SerializeField] private Interactable[] interactables;

        [Header("UI")]
        [SerializeField] private HudWidget hud;

        [SerializeField, Tooltip("Full-screen container (under the HUD) where nameplates and cards are spawned.")]
        private RectTransform worldLayer;

        [SerializeField] private CharacterWidget nameplatePrefab;

        [SerializeField] private InteractionWidget cardPrefab;

        [SerializeField, Tooltip("Gap between the top of an object and its nameplate or card, in meters.")]
        private float gap = 0.35f;

        private GameViewModel _game;

        // Start runs after every Awake, so the characters already created their stats.
        private void Start()
        {
            var chests = 0;

            foreach (var interactable in interactables)
            {
                if (interactable is Chest)
                {
                    chests++;
                }
            }

            _game = new GameViewModel(player.Stats, chests);

            player.Init(_game);

            player.SetWorld(enemies, interactables);

            hud.SetViewModel(_game);

            Spawn(nameplatePrefab, player.transform).SetViewModel(player.Stats);

            foreach (var enemy in enemies)
            {
                enemy.Init(_game);

                enemy.SetTarget(player);

                Spawn(nameplatePrefab, enemy.transform).SetViewModel(enemy.Stats);
            }

            foreach (var interactable in interactables)
            {
                interactable.Init(_game);

                Spawn(cardPrefab, interactable.transform).SetViewModel(interactable.Prompt);
            }
        }

        private T Spawn<T>(T prefab, Transform target) where T : Widget
        {
            var widget = Instantiate(prefab, worldLayer);

            widget.name = prefab.name + " (" + target.name + ")";

            widget.GetComponent<WorldAnchor>().Follow(target, new Vector3(0f, HeightOf(target) + gap, 0f));

            return widget;
        }

        // Height of the object above its pivot, from its renderers.
        private static float HeightOf(Transform target)
        {
            var top = target.position.y;

            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                top = Mathf.Max(top, renderer.bounds.max.y);
            }

            return top - target.position.y;
        }
    }
}
