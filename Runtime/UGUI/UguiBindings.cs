using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// UGUI bindings. Inside a <see cref="Widget{TViewModel}"/>: <c>Bind.Fill(healthBar, vm.Health);</c>
    /// Input widgets (Slider, Toggle, Dropdown...) are two-way when given an <see cref="Observable{T}"/>
    /// and one-way when given an <see cref="IReadOnlyObservable{T}"/> (use <c>.AsReadOnly()</c> to force it).
    /// </summary>
    public static class UguiBindings
    {
#region One-way

        public static void Text(this BindingScope bind, Text label, IReadOnlyObservable<string> source)
        {
            bind.To(source, label, static (target, value) => target.text = value);
        }

        public static void Fill(this BindingScope bind, Image image, IReadOnlyObservable<float> source)
        {
            bind.To(source, image, static (target, value) => target.fillAmount = value);
        }

        public static void Sprite(this BindingScope bind, Image image, IReadOnlyObservable<Sprite> source)
        {
            bind.To(source, image, static (target, value) => target.sprite = value);
        }

        public static void Color(this BindingScope bind, Graphic graphic, IReadOnlyObservable<Color> source)
        {
            bind.To(source, graphic, static (target, value) => target.color = value);
        }

        public static void Interactable(this BindingScope bind, Selectable selectable, IReadOnlyObservable<bool> source)
        {
            bind.To(source, selectable, static (target, value) => target.interactable = value);
        }

        public static void Active(this BindingScope bind, GameObject gameObject, IReadOnlyObservable<bool> source)
        {
            bind.To(source, gameObject, static (target, value) => target.SetActive(value));
        }

        public static void Active(this BindingScope bind, Component component, IReadOnlyObservable<bool> source)
        {
            bind.Active(component.gameObject, source);
        }

        public static void Alpha(this BindingScope bind, CanvasGroup group, IReadOnlyObservable<float> source)
        {
            bind.To(source, group, static (target, value) => target.alpha = value);
        }

#endregion

#region Input widgets

        public static void Value(this BindingScope bind, Slider slider, IReadOnlyObservable<float> source)
        {
            bind.To(source, slider, static (target, value) => target.SetValueWithoutNotify(value));
        }

        public static void Value(this BindingScope bind, Slider slider, Observable<float> source)
        {
            bind.Value(slider, source.AsReadOnly());

            bind.Event(slider.onValueChanged, source);
        }

        public static void Value(this BindingScope bind, Scrollbar scrollbar, IReadOnlyObservable<float> source)
        {
            bind.To(source, scrollbar, static (target, value) => target.SetValueWithoutNotify(value));
        }

        public static void Value(this BindingScope bind, Scrollbar scrollbar, Observable<float> source)
        {
            bind.Value(scrollbar, source.AsReadOnly());

            bind.Event(scrollbar.onValueChanged, source);
        }

        public static void Value(this BindingScope bind, Dropdown dropdown, IReadOnlyObservable<int> source)
        {
            bind.To(source, dropdown, static (target, value) => target.SetValueWithoutNotify(value));
        }

        public static void Value(this BindingScope bind, Dropdown dropdown, Observable<int> source)
        {
            bind.Value(dropdown, source.AsReadOnly());

            bind.Event(dropdown.onValueChanged, source);
        }

        public static void IsOn(this BindingScope bind, Toggle toggle, IReadOnlyObservable<bool> source)
        {
            bind.To(source, toggle, static (target, value) => target.SetIsOnWithoutNotify(value));
        }

        public static void IsOn(this BindingScope bind, Toggle toggle, Observable<bool> source)
        {
            bind.IsOn(toggle, source.AsReadOnly());

            bind.Event(toggle.onValueChanged, source);
        }

        public static void Text(this BindingScope bind, InputField input, IReadOnlyObservable<string> source)
        {
            bind.To(source, input, static (target, value) => target.SetTextWithoutNotify(value));
        }

        public static void Text(this BindingScope bind, InputField input, Observable<string> source)
        {
            bind.Text(input, source.AsReadOnly());

            bind.Event(input.onValueChanged, source);
        }

#endregion

#region Commands

        /// <summary>
        /// Executes <paramref name="command"/> on click and keeps <c>button.interactable</c> in sync with <see cref="Command.CanExecute"/>.
        /// </summary>
        public static void Click(this BindingScope bind, Button button, Command command)
        {
            bind.Interactable(button, command.CanExecute);

            bind.Event(button.onClick, command);
        }

#endregion
    }
}
