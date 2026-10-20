using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class UguiBindingTests : UiTestBase
    {
        private BindingScope _scope;

        [SetUp]
        public void CreateScope()
        {
            _scope = new BindingScope();
        }

        [TearDown]
        public void DisposeScope()
        {
            _scope.Dispose();
        }

        [Test]
        public void LegacyText()
        {
            var label = Create<Text>();

            var source = new Observable<string>("a");

            _scope.Text(label, source);

            source.Value = "b";

            Assert.AreEqual("b", label.text);
        }

        [Test]
        public void ImageFill()
        {
            var image = Create<Image>();

            var source = new Observable<float>(0.2f);

            _scope.Fill(image, source);

            Assert.AreEqual(0.2f, image.fillAmount);
        }

        [Test]
        public void ImageSprite()
        {
            var image = Create<Image>();

            var sprite = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            var source = new Observable<Sprite>();

            _scope.Sprite(image, source);

            source.Value = sprite;

            Assert.AreSame(sprite, image.sprite);

            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void GraphicColor()
        {
            var image = Create<Image>();

            var source = new Observable<Color>(UnityEngine.Color.green);

            _scope.Color(image, source);

            Assert.AreEqual(UnityEngine.Color.green, image.color);
        }

        [Test]
        public void SelectableInteractable()
        {
            var button = Create<Button>();

            var source = new Observable<bool>(false);

            _scope.Interactable(button, source);

            Assert.IsFalse(button.interactable);
        }

        [Test]
        public void GameObjectActive()
        {
            var panel = Create<Image>();

            var source = new Observable<bool>(true);

            _scope.Active(panel, source);

            source.Value = false;

            Assert.IsFalse(panel.gameObject.activeSelf);
        }

        [Test]
        public void CanvasGroupAlpha()
        {
            var group = Create<CanvasGroup>();

            var source = new Observable<float>(0.5f);

            _scope.Alpha(group, source);

            Assert.AreEqual(0.5f, group.alpha);
        }

        [Test]
        public void Scrollbar_TwoWay()
        {
            var scrollbar = Create<Scrollbar>();

            var source = new Observable<float>(0.25f);

            _scope.Value(scrollbar, source);

            Assert.AreEqual(0.25f, scrollbar.value);

            scrollbar.value = 0.75f;

            Assert.AreEqual(0.75f, source.Value);
        }

        [Test]
        public void Toggle_TwoWay()
        {
            var toggle = Create<Toggle>();

            var source = new Observable<bool>(true);

            _scope.IsOn(toggle, source);

            Assert.IsTrue(toggle.isOn);

            toggle.isOn = false;

            Assert.IsFalse(source.Value);
        }

        [Test]
        public void Dropdown_TwoWay()
        {
            var dropdown = CreateDropdown();

            var source = new Observable<int>(1);

            _scope.Value(dropdown, source);

            Assert.AreEqual(1, dropdown.value);

            dropdown.value = 2;

            Assert.AreEqual(2, source.Value);
        }

        [Test]
        public void InputField_TwoWay()
        {
            var input = Create<InputField>();

            var source = new Observable<string>("hola");

            _scope.Text(input, source);

            Assert.AreEqual("hola", input.text);

            input.text = "adiós";

            Assert.AreEqual("adiós", source.Value);
        }
    }
}
