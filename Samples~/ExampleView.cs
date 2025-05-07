using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverMV
{
    public class ExampleView : MonoBehaviour
    {
        public Observer observer;

        [Bindable("Name")] public TMP_Text text;
        [Bindable("Picture")] public Image picture;
        [Bindable("Health")] public Slider health;
        [Bindable("Button")] public Button button;

        private void Start()
        {
            observer.AutoBind(this);

            observer.SubscribeViewChanges(OnViewValueChanged);
        }

        private void OnViewValueChanged(string key, object value)
        {
            if (key == "Health" && value is float hp)
            {
                health.value = hp;

                Debug.Log($"Incoming Value: {hp} :: Slider Value: {health.value}");
            }
        }
    }
}
