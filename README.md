# 📘 ObserverVM – Unity UI Data Binding Framework
ObserverVM is a lightweight and extensible data binding system for Unity's UI (UGUI). It bridges the gap between the ViewModel and the UI View by automating the flow of data — either one-way (ViewModel → UI) or two-way (UI → ViewModel).

## 🚀 Getting Started

### 1. Initialization

Before using Observer, you must call `Observer.Init()` with a BindingRegistry instance that contains all registered binding strategies.

```csharp
void Awake()
{
    var registry = new BindingRegistry();
    
    registry.Register(new TextBindingStrategy(), 
                      new SliderBindingStrategy(),
                      new ButtonBindingStrategy());
    
    // Add more strategies as needed...

    Observer.Init(registry);
}

```
⚠️ `Observer.Init()` is mandatory. If not called, no bindings will be functional. Only call once.

### 2. Core Components

| Component           | Description                                                              |
| ------------------- | ------------------------------------------------------------------------ |
| `Observer`          | Central class that manages binding between ViewModel and View            |
| `BindableAttribute` | Attribute used to mark fields that should be bound                       |
| `IBindingStrategy`  | Interface that defines how to update a UI component                      |
| `BindingRegistry`   | Container that maps UI components to their associated `IBindingStrategy` |

## 💡 How It Works

You annotate UI components in your MonoBehaviour using the `[Bindable("KeyName")]` attribute.

You call `observer.AutoBind(this)` to collect those references.

You use `observer.UpdateValue("KeyName", value)` to push values to the UI.

(Optional) You call observer.SubscribeViewChanges(OnChanged) to receive values when the user interacts with UI (for two-way binding).

## 💡 Example Use Case

### View

```csharp
public class PlayerUI : MonoBehaviour
{
    public Observer observer;

    [Bindable("Nickname")] public TMP_Text nicknameText;
    [Bindable("Score")] public Slider scoreSlider;

    void Start()
    {
        observer.AutoBind(this);
        observer.SubscribeViewChanges(OnUIChanged);
    }

    void OnUIChanged(string key, object value)
    {
        if (key == "Score" && value is float score)
        {
            Debug.Log($"User changed score to {score}");
        }
    }
}

```

### ViewModel

```csharp
public class PlayerViewModel : MonoBehaviour
{
    [SerializeField] private Observer observer;

    [SerializeField] private string nickname;
    [SerializeField] private float score;

    public void RefreshUI()
    {
        observer.UpdateValue("Nickname", nickname);
        observer.UpdateValue("Score", score);
    }
}

```

## 🧱 Creating a Custom Strategy

To support a new UI component type (e.g., Button, Toggle, Image), implement the IBindingStrategy interface:

```csharp
 public class SliderBindingStrategy : IBidirectionalBindingStrategy
    {
        public bool Supports(Component component) => component is Slider;

        public void UpdateBinding(Component component, object value)
        {
            if (component is Slider slider && value is float f)
            {
                slider.SetValueWithoutNotify(f);
            }
        }
        
        public void BindViewEvent(Component component, Action<string, object> onViewChanged, string key)
        {
            if (component is Slider slider)
            {
                slider.onValueChanged.AddListener(value =>
                {
                    onViewChanged?.Invoke(key, value);
                });
            }
        }
    }
```

## ✅ Summary

- Use `[Bindable("Key")]` on fields inside your UI scripts.
- Use `observer.AutoBind(this)` to register them at runtime.
- Use `observer.UpdateValue("Key", value)` to push data to the UI.
- Use `observer.SubscribeViewChanges()` to enable two-way communication.
- You must call `Observer.Init(registry)` and register all relevant strategies.
- You can extend the system by creating new `IBindingStrategy` implementations.