# 📘 ObserverVM – Unity UI Data Binding Framework
ObserverVM is a lightweight and extensible data binding system for Unity's UI (UGUI). It bridges the gap between the ViewModel and the UI View by automating the flow of data — either one-way (ViewModel → UI) or two-way (UI → ViewModel).

## 🚀 Getting Started

### 1. Initialization

Before using Observer, you must call `Observer.Init()` with a BindingRegistry instance that contains all registered binding strategies.

```csharp
void Awake()
{
    var strategies = new BindingRegistry
    {
        new TextBindingStrategy(), 
        new SliderBindingStrategy(),
        new ButtonBindingStrategy())
    };
    
    // Add more strategies as needed...

    Observer.Init(registry);
}
```

⚠️ `Observer.Init()` is mandatory. If not called, no bindings will be functional. Only call once.

### 2. Core Components

| Component         | Description                                                              |
|-------------------|--------------------------------------------------------------------------|
| Observer          | Central class that manages binding between ViewModel and View            |
| BindableAttribute | Attribute used to mark fields that should be bound                       |
| IBindingStrategy  | Interface that defines how to update a UI component                      |
| BindingRegistry   | Container that maps UI components to their associated `IBindingStrategy` |
| BindingKey        | Strongly-typed, hashable key for optimized binding instead of strings    |


## 💡 How It Works

You annotate UI components in your MonoBehaviour using the `[Bindable("KeyName")]` attribute.

You call `observer.AutoBind(this)` to collect those references automatically.

You use `observer.UpdateValue("KeyName", value)` to push values to the UI.

⚠️ For `IBidirectionalBindingStrategy` strategies you call `observer.SubscribeViewChanges()` to receive values when the user interacts with the UI.

## 🧱 Creating a Custom Strategy

To support a new UI component type (e.g., Button, Toggle, Image), implement the `IBindingStrategy` or `IBidirectionalBindingStrategy` interface:

### One-way `IBindingStrategy`

```csharp
public class TextBindingStrategy : IBindingStrategy
    {
        public bool Supports(Component component) => component is TMP_Text;

        public void UpdateBinding(Component component, object value)
        {
            if (component is TMP_Text text && value is string str)
            {
                text.text = str;
            }
        }
    }
```

### Two-way `IBidirectionalBindingStrategy`

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

## 🆕 What’s New in Version 1.1.0

### 1. ⚡ BindingKey: Strongly-Typed, Hashed Bind Keys

Version `1.1.0` introduces `BindingKey`, a strongly-typed and hashable class to replace strings for binding keys. This provides a performance improvement by avoiding string comparisons at runtime.

### Declaring a `BindingKey`

```csharp
public readonly static BindingKey HealthBar = new("PlayerHealthBar");
```

### Manual Binding with `BindingKey`

```csharp
[SerializeField]
private Image healthBar;

observer.Bind(HealthBar, healthBar);
```

### Optional: Use `[Bindable]` for Visual Marking in Inspector

Even though it's not required when using `BindingKey`, you may still decorate fields with `[Bindable]` (without a name) to mark them visually in the Unity Inspector.

```csharp
[Bindable, SerializeField]
private Image healthBar;

observer.Bind(HealthBar, healthBar);
```

️️️️⚠️ If you provide a name inside `[Bindable("SomeName")]`, the variable will be registered via `observer.AutoBind()` using that name.

### Updating a Bound UI Element

```csharp
observer.UpdateValue(HealthBar, value);
```

This replaces the use of:

```csharp
observer.UpdateValue("PlayerHealthBar", value);
```

## ✅ Summary

- Use `[Bindable("Key")]` to mark fields to be auto-bound via `observer.AutoBind(this)`.
- You can also use `BindingKey` for optimized and strongly-typed bindings.
- Use `observer.Bind(key, component)` to manually bind a component using `BindingKey`.
- Use `observer.UpdateValue(key, value)` to push data to the UI, using either `string` or `BindingKey`.
- Use `observer.SubscribeViewChanges()` only if using bidirectional binding strategies.
- Extend the system by creating new `IBindingStrategy` or `IBidirectionalBindingStrategy` implementations.
- Call `Observer.Init(registry)` and register all relevant strategies before binding.
