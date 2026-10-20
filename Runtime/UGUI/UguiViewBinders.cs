using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM
{
    // The UGUI properties View Bindings can target. Same bindings as the Bind.* methods, so they share the zero-allocation guarantees.
    internal static class UguiViewBinders
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            ViewBinders.Register<Text, string>("Text", static (bind, label, source) => bind.Text(label, source));

            ViewBinders.Register<Image, float>("Fill", static (bind, image, source) => bind.Fill(image, source));

            ViewBinders.Register<Image, Sprite>("Sprite", static (bind, image, source) => bind.Sprite(image, source));

            ViewBinders.Register<Graphic, Color>("Color", static (bind, graphic, source) => bind.Color(graphic, source));

            ViewBinders.Register<CanvasGroup, float>("Alpha", static (bind, group, source) => bind.Alpha(group, source));

            ViewBinders.Register<Selectable, bool>("Interactable",
                                                   static (bind, selectable, source) => bind.Interactable(selectable, source),
                                                   inverted: static (bind, selectable, source) => bind.To(source, selectable, static (target, value) => target.interactable = !value));

            ViewBinders.Register<GameObject, bool>("Active",
                                                   static (bind, gameObject, source) => bind.Active(gameObject, source),
                                                   inverted: static (bind, gameObject, source) => bind.To(source, gameObject, static (target, value) => target.SetActive(!value)));

            ViewBinders.Register<Component, bool>("Active",
                                                  static (bind, component, source) => bind.Active(component, source),
                                                  inverted: static (bind, component, source) => bind.To(source, component.gameObject, static (target, value) => target.SetActive(!value)));

            ViewBinders.Register<Slider, float>("Value",
                                                static (bind, slider, source) => bind.Value(slider, source),
                                                twoWay: static (bind, slider, source) => bind.Value(slider, source));

            ViewBinders.Register<Scrollbar, float>("Value",
                                                   static (bind, scrollbar, source) => bind.Value(scrollbar, source),
                                                   twoWay: static (bind, scrollbar, source) => bind.Value(scrollbar, source));

            ViewBinders.Register<Dropdown, int>("Value",
                                                static (bind, dropdown, source) => bind.Value(dropdown, source),
                                                twoWay: static (bind, dropdown, source) => bind.Value(dropdown, source));

            ViewBinders.Register<Toggle, bool>("IsOn",
                                               static (bind, toggle, source) => bind.IsOn(toggle, source),
                                               twoWay: static (bind, toggle, source) => bind.IsOn(toggle, source));

            ViewBinders.Register<InputField, string>("Text",
                                                     static (bind, input, source) => bind.Text(input, source),
                                                     twoWay: static (bind, input, source) => bind.Text(input, source));

            ViewBinders.RegisterCommand<Button>("Click", static (bind, button, command) => bind.Click(button, command));
        }
    }
}
