#if OBSERVERVM_TMP
using TMPro;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    // The TextMeshPro properties View Bindings can target. Numbers accept a format ("{0}", "{0:2}", "Score {0}").
    internal static class TmpViewBinders
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            ViewBinders.Register<TMP_Text, string>("Text", static (bind, label, source) => bind.Text(label, source));

            ViewBinders.Register<TMP_Text, int>("Text",
                                                static (bind, label, source) => bind.Text(label, source),
                                                formatted: static (bind, label, source, format) => bind.Text(label, source, format));

            ViewBinders.Register<TMP_Text, float>("Text",
                                                  static (bind, label, source) => bind.Text(label, source),
                                                  formatted: static (bind, label, source, format) => bind.Text(label, source, format));

            ViewBinders.Register<TMP_InputField, string>("Text",
                                                         static (bind, input, source) => bind.Text(input, source),
                                                         twoWay: static (bind, input, source) => bind.Text(input, source));

            ViewBinders.Register<TMP_Dropdown, int>("Value",
                                                    static (bind, dropdown, source) => bind.Value(dropdown, source),
                                                    twoWay: static (bind, dropdown, source) => bind.Value(dropdown, source));
        }
    }
}
#endif
