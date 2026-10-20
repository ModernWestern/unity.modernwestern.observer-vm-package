using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // Keeps a screen-space UI element over a point of the 3D world: a nameplate over a character, a card over a chest.
    // It is not a binding: the position changes every frame, so it just projects the point in LateUpdate, without allocating.
    // Works with Screen Space - Overlay and Screen Space - Camera canvases.
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldAnchor : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [SerializeField, Tooltip("World-space offset from the target, e.g. above its head.")]
        private Vector3 offset = new(0f, 1.5f, 0f);

        [SerializeField, Tooltip("Camera that sees the world. Empty = Camera.main.")]
        private Camera worldCamera;

        private RectTransform _rect;

        private Canvas _canvas;

        public void Follow(Transform newTarget, Vector3 newOffset)
        {
            target = newTarget;

            offset = newOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (_canvas == null)
            {
                _rect = (RectTransform)transform;

                _canvas = GetComponentInParent<Canvas>().rootCanvas;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            var screenPoint = worldCamera.WorldToScreenPoint(target.position + offset);

            // Behind the camera: hide it instead of showing it mirrored.
            var visible = screenPoint.z > 0f;

            _rect.localScale = visible ? Vector3.one : Vector3.zero;

            if (!visible)
            {
                return;
            }

            var uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_rect.parent, screenPoint, uiCamera, out var localPoint);

            _rect.localPosition = localPoint;
        }
    }
}
