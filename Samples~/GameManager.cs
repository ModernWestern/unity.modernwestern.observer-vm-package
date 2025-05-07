using UnityEngine;
using ModernWestern.UI.ObserverMV;
using ModernWestern.UI.ObserverMV.Strategy;

public class GameManager : MonoBehaviour
{
    private BindingRegistry _registry;

    private void Awake()
    {
        _registry = new BindingRegistry();

        _registry.Register(new TextBindingStrategy(),
                           new ImageBindingStrategy(),
                           new SliderBindingStrategy(),
                           new ButtonBindingStrategy());

        Observer.Init(_registry);
    }

    private void OnDestroy()
    {
        Observer.DeInit();
    }
}
