using System;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM
{
    /// <summary>
    /// Marks a widget reference a <see cref="Widget{TViewModel}"/> needs, like Unreal's <c>meta=(BindWidget)</c>.
    /// The Inspector highlights it in red while it is empty, unless <see cref="Optional"/> is set (<c>BindWidgetOptional</c>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class BindWidgetAttribute : PropertyAttribute
    {
        public bool Optional { get; set; }
    }
}
