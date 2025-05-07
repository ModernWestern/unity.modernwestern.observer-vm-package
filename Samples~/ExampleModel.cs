using UnityEngine;
using UnityEngine.InputSystem;

namespace ModernWestern.UI.ObserverMV
{
    public class ExampleModel : MonoBehaviour
    {
        [SerializeField]
        private Observer observer;

        [SerializeField]
        private string text;

        [SerializeField]
        private Sprite picture;

        [SerializeField, Range(0, 1)]
        private float health;

        [SerializeField]
        private Color color = Color.white;
        
        [SerializeField]
        private InputActionReference input;

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        // private void OnEnable()
        // {
        //     input.action.Enable();
        //     input.action.performed += OnClick;
        // }
        //
        // private void OnDisable()
        // {
        //     input.action.Disable();
        //     input.action.performed -= OnClick;
        // }

        public void UpdateName(string value) => observer.UpdateValue("Name", value);
        public void UpdatePicture(Sprite sprite) => observer.UpdateValue("Picture", sprite);
        public void UpdateHealth(float hp) => observer.UpdateValue("Health", hp);
        public void UpdateButton(object value) => observer.UpdateValue("Button", value);

        [ContextMenu("Update All")]
        private void UpdateAll()
        {
            UpdateName(text);
            UpdatePicture(picture);
            UpdateHealth(health);
            UpdateButton(color);
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            var pos = context.ReadValue<Vector2>();
            
            UpdateButton(pos);
            
            Debug.Log(pos);
        }
    }
}
