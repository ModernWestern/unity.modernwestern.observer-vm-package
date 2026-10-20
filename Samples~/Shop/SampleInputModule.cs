using UnityEngine;
using UnityEngine.EventSystems;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ModernWestern.UI.ObserverVM.Samples.Shop
{
    // Adds the UI input module that matches the project's Active Input Handling,
    // so the sample works with the legacy Input Manager and with the Input System package.
    [RequireComponent(typeof(EventSystem))]
    public sealed class SampleInputModule : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<BaseInputModule>() != null)
            {
                return;
            }

#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            gameObject.AddComponent<InputSystemUIInputModule>();
#else
            gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
