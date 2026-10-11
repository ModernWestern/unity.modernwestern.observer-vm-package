using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public abstract class UiTestBase
    {
        private readonly List<GameObject> _created = new();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            foreach (var gameObject in _created)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            _created.Clear();
        }

        protected GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));

            _created.Add(gameObject);

            return gameObject;
        }

        protected T Create<T>() where T : Component
        {
            return CreateObject(typeof(T).Name).AddComponent<T>();
        }

        protected Dropdown CreateDropdown()
        {
            var dropdown = Create<Dropdown>();

            dropdown.options.Add(new Dropdown.OptionData("Low"));

            dropdown.options.Add(new Dropdown.OptionData("Medium"));

            dropdown.options.Add(new Dropdown.OptionData("High"));

            return dropdown;
        }

        protected TestWidget CreateWidget()
        {
            var widget = Create<TestWidget>();

            widget.HealthBar = Create<Image>();

            widget.Volume = Create<Slider>();

            widget.Muted = Create<Toggle>();

            widget.Quality = CreateDropdown();

            widget.Nickname = Create<Text>();

            widget.Respawn = Create<Button>();

            widget.Panel = CreateObject("Panel");

            return widget;
        }
    }
}
