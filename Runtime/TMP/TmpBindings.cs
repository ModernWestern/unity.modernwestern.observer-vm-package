#if OBSERVERVM_TMP
using TMPro;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// TextMeshPro bindings. Numbers go through <c>TMP_Text.SetText(format, value)</c>, which builds the text without creating strings
    /// (in player builds; TMP itself allocates inside the Editor to show the text in the Inspector).
    /// Format examples: <c>"{0}"</c>, <c>"{0:2}"</c> (2 decimals), <c>"HP {0:0}"</c>.
    /// </summary>
    public static class TmpBindings
    {
#region One-way

        public static void Text(this BindingScope bind, TMP_Text label, IReadOnlyObservable<string> source)
        {
            bind.To(source, label, static (target, value) => target.text = value);
        }

        public static void Text(this BindingScope bind, TMP_Text label, IReadOnlyObservable<int> source, string format = "{0}")
        {
            bind.To(source, label, format, static (target, text, value) => target.SetText(text, value));
        }

        public static void Text(this BindingScope bind, TMP_Text label, IReadOnlyObservable<float> source, string format = "{0}")
        {
            bind.To(source, label, format, static (target, text, value) => target.SetText(text, value));
        }

#endregion

#region Input widgets

        public static void Text(this BindingScope bind, TMP_InputField input, IReadOnlyObservable<string> source)
        {
            bind.To(source, input, static (target, value) => target.SetTextWithoutNotify(value));
        }

        public static void Text(this BindingScope bind, TMP_InputField input, Observable<string> source)
        {
            bind.Text(input, source.AsReadOnly());

            bind.Event(input.onValueChanged, source);
        }

        public static void Value(this BindingScope bind, TMP_Dropdown dropdown, IReadOnlyObservable<int> source)
        {
            bind.To(source, dropdown, static (target, value) => target.SetValueWithoutNotify(value));
        }

        public static void Value(this BindingScope bind, TMP_Dropdown dropdown, Observable<int> source)
        {
            bind.Value(dropdown, source.AsReadOnly());

            bind.Event(dropdown.onValueChanged, source);
        }

#endregion
    }
}
#endif
