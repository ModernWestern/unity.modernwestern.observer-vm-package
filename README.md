# ObserverVM

Zero-allocation, strongly typed MVVM bindings for Unity UI (UGUI + TextMeshPro), inspired by Unreal's UMG.

- **Views** derive from `Widget<TViewModel>` (≈ `UUserWidget`) and mark their UI with `[BindWidget]`.
- **ViewModels** are plain classes that expose `Observable<T>` properties and `Command`s. They know nothing about the UI.
- **Bindings** are declared once in `OnBind` with `Bind.Text`, `Bind.Value`, `Bind.Click`...
- **No GC**: changing values, user input, unbinding and re-binding a widget to another ViewModel allocate nothing.
- **Type safe**: binding a `float` to a text or a `string` to a slider does not compile.

| Unreal (UMG) | ObserverVM |
|---|---|
| `UUserWidget` | `Widget<TViewModel>` |
| `meta=(BindWidget)` / `BindWidgetOptional` | `[BindWidget]` / `[BindWidget(Optional = true)]`, with **Auto-assign by name** |
| Widget Blueprint does not compile with a missing BindWidget | Play warns and the build fails (configurable) |
| ViewModel with `FieldNotify` properties | class with `Observable<T>` properties |
| View Bindings panel | **View Bindings** in the widget inspector (no code), or `OnBind(viewModel)` + `Bind.*` |
| `SetViewModel` | `SetViewModel` |
| Button `OnClicked` → ViewModel function | `Command` / `Command<T>` + `Bind.Click` |
| `ListView` with entry widgets | `ObservableList<T>` + `Bind.List` |
| Nested widget with its own ViewModel | `Bind.Child` |

## Installation

Package Manager → **Add package from git URL**:

```
https://github.com/ModernWestern/unity.modernwestern.observer-vm-package.git
```

Requires Unity 2021.3+ and UGUI. TextMeshPro bindings are enabled automatically when TextMeshPro is installed (always the case in Unity 6).

## Quick start

### ViewModel

```csharp
public sealed class PlayerViewModel
{
    private readonly Observable<float> _health = new(1f);

    public PlayerViewModel(string nickname)
    {
        Nickname = new Observable<string>(nickname);
        Respawn = new Command(OnRespawn, canExecute: false);
    }

    public Observable<string> Nickname { get; }
    public IReadOnlyObservable<float> Health => _health;   // read-only: widgets can only display it
    public Observable<int> Score { get; } = new();
    public Observable<float> Volume { get; } = new(0.8f);  // writable: a Slider bound to it is two-way
    public Command Respawn { get; }

    public void TakeDamage(float amount)
    {
        _health.Value = Mathf.Max(0f, _health.Value - amount);
        Respawn.CanExecute.Value = _health.Value <= 0f;
    }

    private void OnRespawn() => _health.Value = 1f;
}
```

### Widget (the View)

```csharp
public sealed class PlayerHUD : Widget<PlayerViewModel>
{
    [SerializeField, BindWidget] private TMP_Text nickname;
    [SerializeField, BindWidget] private TMP_Text score;
    [SerializeField, BindWidget] private Image healthBar;
    [SerializeField, BindWidget] private Slider volume;
    [SerializeField, BindWidget] private Button respawn;

    protected override void OnBind(PlayerViewModel viewModel)
    {
        Bind.Text(nickname, viewModel.Nickname);
        Bind.Text(score, viewModel.Score, "Score {0}");  // numbers to text without creating strings
        Bind.Fill(healthBar, viewModel.Health);
        Bind.Value(volume, viewModel.Volume);            // two-way
        Bind.Click(respawn, viewModel.Respawn);          // also keeps button.interactable = CanExecute

        // Custom binding: a static lambda plus the state it needs, so nothing is captured.
        Bind.To(viewModel.Health, this, static (self, health) => self.OnHealthChanged(health));
    }

    private void OnHealthChanged(float health) { /* ... */ }
}
```

### Gameplay

```csharp
var player = new PlayerViewModel("Player");
hud.SetViewModel(player);   // binds; calling it again with another ViewModel re-binds, null unbinds
player.TakeDamage(0.1f);    // the HUD updates, no allocations
```

Bindings are removed automatically when the ViewModel changes or the widget is destroyed. Override `OnUnbind(viewModel)` for extra cleanup; if you override `OnDestroy`, call `base.OnDestroy()`.

## Samples

Package Manager → **ObserverVM** → **Samples** → **Import**, open the scene and press Play. Both need TextMeshPro: if Unity asks to import *TMP Essential Resources*, accept. They work with the legacy Input Manager and with the Input System package.

### Shop (start here)

Where the binding pays off: one value drives many widgets, and nobody has to refresh the UI.

- `Wallet` holds the gold. Every `ShopItem` derives "can I afford it?" from it (`Buy.CanExecute`) with `Watch`, which stops when the item is disposed.
- `ShopViewModel` has the wallet and an `ObservableList<ShopItem>`; `ShopWidget` shows it with `Bind.List` (one row per item from a single row prefab) and hands the wallet to its own widget with `Bind.Child`.
- `ShopDemo` builds the shop from a catalog (editable in the Inspector) and adds income every second by changing `Wallet.Gold` only. The gold counter and every Buy button update by themselves.

```csharp
// ShopItem (ViewModel): affordability follows the wallet, whoever changes the gold.
Watch(wallet.Gold, this, static (item, gold) => item.Buy.CanExecute.Value = gold >= item.Price);

// ShopWidget (View): a nested widget and a list, like UMG's child widgets and ListView.
Bind.Child(wallet, shop.Wallet);
Bind.List(rowTemplate, shop.Items);

// ShopItemWidget (View): constants are plain assignments, only what changes is bound.
itemName.text = item.Name;
Bind.Text(owned, item.Owned, "x{0}");
Bind.Click(buy, item.Buy);   // greys out while the item is not affordable
```

### Basic HUD

A player HUD bound almost entirely from the Inspector (**View Bindings**): nickname, score, health bar, two-way volume slider and mute toggle, "mute disables the slider" (inverted), `Hit` / `Respawn` commands and the death screen. `PlayerHUD.OnBind` keeps only the two bindings that need a calculation (the percentages and the health color).

## Bindings

Input widgets are **two-way** when given an `Observable<T>` and **one-way** when given an `IReadOnlyObservable<T>`. Use `.AsReadOnly()` to force one-way.

| Binding | Widget | Source |
|---|---|---|
| `Bind.Text` | `Text`, `TMP_Text` | `string` |
| `Bind.Text(label, source, "format")` | `TMP_Text` | `int`, `float` (e.g. `"{0}"`, `"{0:2}"`, `"HP {0:0}"`) |
| `Bind.Fill` | `Image` | `float` |
| `Bind.Sprite` | `Image` | `Sprite` |
| `Bind.Color` | `Graphic` (Image, Text, TMP_Text...) | `Color` |
| `Bind.Interactable` | `Selectable` | `bool` |
| `Bind.Active` | `GameObject`, `Component` | `bool` |
| `Bind.Alpha` | `CanvasGroup` | `float` |
| `Bind.Value` | `Slider`, `Scrollbar` | `float` (two-way) |
| `Bind.Value` | `Dropdown`, `TMP_Dropdown` | `int` (two-way) |
| `Bind.IsOn` | `Toggle` | `bool` (two-way) |
| `Bind.Text` | `InputField`, `TMP_InputField` | `string` (two-way) |
| `Bind.Click` | `Button` | `Command`, or `Command<T>` + a parameter |
| `Bind.List` | a row template (`Widget<TItem>`) | `ObservableList<TItem>` |
| `Bind.Child` | a nested `Widget<TChild>` | a `TChild`, or an `IReadOnlyObservable<TChild>` (e.g. the selected item) |
| `Bind.To` | anything | any `T`, with a `static` lambda |
| `Bind.Event` | any `UnityEvent<T>` / `UnityEvent` | `Observable<T>` / `Command` (View → ViewModel) |

### Lists

`ObservableList<T>` notifies adds, removes, replacements, moves and clears. `Bind.List(template, items)` hides the template and keeps one copy per item next to it (put the template alone under its container, e.g. a `VerticalLayoutGroup`), named `Row 1`, `Row 2`... for a template called `Row Template`. Rows are pooled: removing an item hides its row and keeps it for the next one, and re-binding to another list reuses them.

```csharp
public ObservableList<ShopItem> Items { get; } = new();

// In the widget:
Bind.List(rowTemplate, viewModel.Items);   // rowTemplate is a ShopItemWidget : Widget<ShopItem>
```

### Child widgets

`Bind.Child(childWidget, viewModel.Part)` gives a nested widget its ViewModel while the parent is bound and clears it when the parent unbinds. Pass an `IReadOnlyObservable<TChild>` to follow a changing ViewModel, such as a details panel showing the selected item.

### Derived state and ViewModel lifetime

A ViewModel that reacts to another one (derived state) should stop listening when it is thrown away. Derive from `ViewModel` and use `Watch`: every subscription is released by `Dispose()`.

```csharp
public sealed class ShopItem : ViewModel
{
    public ShopItem(Wallet wallet, int price)
    {
        Price = price;
        Buy = new Command(OnBuy);
        Watch(wallet.Gold, this, static (item, gold) => item.Buy.CanExecute.Value = gold >= item.Price);
    }
    // ...
}
```

Override `OnDispose()` to dispose child ViewModels (for example the items of a list).

### Your own bindings

Bindings are extension methods on `BindingScope`, so adding one is a one-liner:

```csharp
public static class MyBindings
{
    public static void Width(this BindingScope bind, RectTransform rect, IReadOnlyObservable<float> source)
    {
        bind.To(source, rect, static (target, width) => target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width));
    }
}

// In OnBind:
Bind.Width(panel, viewModel.PanelWidth);
```

## View Bindings (no code)

Like the View Bindings panel of UMG, bindings can be configured in the widget's inspector instead of `OnBind`. A widget can then be just:

```csharp
public sealed class PlayerHUD : Widget<PlayerViewModel> { }
```

Each row of **View Bindings** reads *ViewModel member → widget property*:

- **Member**: a public property or field of the ViewModel that is an `Observable<T>`, `IReadOnlyObservable<T>` or `Command`, listed with its type (`Health (float)`, `Volume (float, writable)`, `Respawn (Command)`).
- **Target**: drag the widget's GameObject or component.
- **Property**: only what fits the member's type, grouped by component (`Image/Fill`, `Slider/Value (two-way)`, `GameObject/Active`, `Button/Click`). Picking one also picks the right component.
- **Options**: *Format* for numbers shown as text (`"Score {0}"`, `"{0:2}"`), *Invert* for true/false values (`Muted` → `Slider/Interactable` inverted).

View Bindings run right after `OnBind`, so both can be combined: keep in code only what needs a calculation. They compile into the same typed bindings as `Bind.*`, so they keep the zero-allocation guarantees (resolved once per widget and cached).

Renaming a ViewModel member breaks the bindings that use it, as in Unreal. The row turns red with the reason, and the validation (Play warning, failing build, Validation window) reports it. For IL2CPP builds, the ViewModel types of your widgets are kept from code stripping automatically (a generated `link.xml`).

Your own widget properties can be offered to View Bindings too:

```csharp
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoadMethod]
#endif
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void RegisterMyBinders()
{
    ViewBinders.Register<RectTransform, float>("Width", static (bind, rect, width) => bind.Width(rect, width));
}
```

Lists (`Bind.List`), child widgets (`Bind.Child`) and parameterized commands are bound in code.

## Editor tools

### Widget inspector

Every `Widget<TViewModel>` gets its own inspector:

- **Status**: which ViewModel it expects, and whether every required widget is bound and every View Binding works.
- **Widgets**: each `[BindWidget]` field with a mark (accent = bound, red = required and empty, grey = optional and empty).
- **View Bindings**: bindings without code (see above).
- **Auto-assign by name**: fills the empty fields with the child named like the field, as Unreal's BindWidget does. `healthBar` finds `Health Bar`, `HealthBar`, `health_bar`, or the name plus a widget word (`Health Bar Image`, `Respawn Button`). It never overwrites a field that is set, leaves a field empty when two children match, does not look inside nested widgets, and supports Undo.
- **ViewModel** (Play mode): the live values of the bound ViewModel. Edit an `Observable<T>` to push a value to the widgets, run a `Command` with its Execute button, and see how many bindings each property has.

### Validation

A required `[BindWidget]` left empty, or a View Binding that cannot work, is reported like a missing BindWidget in Unreal. **Project Settings → ObserverVM** chooses how strict it is:

| When | Default | Options |
|---|---|---|
| Pressing Play | Warn in the Console (click the message to select the object) | Ignore, Warn, Stop |
| Building | Stop: the build fails with the list of empty fields | Ignore, Warn, Stop |
| Check prefabs on build | On: every prefab under `Assets` is checked too | On, Off |

**Window → ObserverVM → Validation** lists every empty required field and broken View Binding in the open scenes and/or prefabs, with **Select** (and **Auto-assign by name** for empty fields) buttons.

### Debugger

**Window → ObserverVM → Debugger** (Play mode) lists every live widget with its ViewModel and binding count. Select a widget there or in the Hierarchy to see and edit its ViewModel.

### New scripts

**Assets → Create → ObserverVM → Widget + ViewModel**: type a name (`Inventory`) and get `InventoryViewModel.cs` and `InventoryWidget.cs`, ready to fill in.

## Zero allocations

**Allocation-free** (verified by `Tests/Runtime/ZeroGcTests.cs` with Unity's `Is.Not.AllocatingGCMemory()`, in the Editor and in Windows players built with Mono and with IL2CPP):

- changing a ViewModel value, with any number of bindings;
- user input flowing back to the ViewModel (sliders, toggles, dropdowns, buttons);
- unbinding (`SetViewModel(null)`, destroying the widget);
- re-binding a widget to a ViewModel it has never seen, for example pooled list items;
- adding, removing, replacing and moving list items bound with `Bind.List`, and re-binding a list, once enough rows exist in its pool.

**One-time costs**, paid once and then reused:

- the first bind of each widget (its binding list, and the UnityEvent listeners, because `UnityEvent.AddListener` allocates inside Unity);
- the first time each `Bind.*` line runs (C# creates the cached `static` lambda);
- the first ViewModel of a kind with 2+ bindings on the same property (later ones reuse its memory from a pool);
- each new row a `Bind.List` needs (it is instantiated once and pooled afterwards).

**Your part:**

- Use `static` lambdas in `Bind.To`. A lambda that captures variables allocates a closure on every bind.
- Show numbers with `Bind.Text(label, source, "{0}")` instead of building strings. Strings you build in your ViewModel are your allocations.
- TextMeshPro's `SetText` is allocation-free in players. Inside the Editor, TextMeshPro allocates a copy of the text for the Inspector.

## ViewModels as ScriptableObjects

A ViewModel can also be a `ScriptableObject` asset, so a HUD in one scene and the gameplay in another can share it without knowing each other. `Observable<T>` is not serialized; if you disable domain reload (*Enter Play Mode Options*), reset its values when play starts.

## Running the tests

Add the package to `testables` in your project's `Packages/manifest.json`:

```json
"testables": [ "com.modernwestern.observer-vm" ]
```

Then run them from the Test Runner (PlayMode), or headless:

```
Unity -batchmode -nographics -projectPath <project> -runTests -testPlatform PlayMode -testResults results.xml
```

## Migrating from 1.x

2.0 replaces the string/`object` API with typed bindings.

| 1.x | 2.0 |
|---|---|
| `Observer` ScriptableObject + `Observer.Init(registry)` | No global state: `widget.SetViewModel(viewModel)` |
| `[Bindable("Key")]` + `observer.AutoBind(this)` | `[SerializeField, BindWidget]` + `OnBind` |
| `observer.UpdateValue("Key", value)` | `viewModel.Property.Value = value` |
| `BindingKey` | Not needed: the property itself is the key |
| `IBindingStrategy` / `BindingRegistry` | Extension methods on `BindingScope` |
| `IBidirectionalBindingStrategy` + `SubscribeViewChanges` | Bind an `Observable<T>` (two-way) or a `Command` |
