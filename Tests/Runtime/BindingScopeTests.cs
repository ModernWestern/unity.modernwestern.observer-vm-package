using NUnit.Framework;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class BindingScopeTests : UiTestBase
    {
        [Test]
        public void Clear_UnbindsEverything()
        {
            var scope = new BindingScope();

            var slider = Create<Slider>();

            var image = Create<Image>();

            var volume = new Observable<float>(0.5f);

            var health = new Observable<float>(1f);

            scope.Value(slider, volume);

            scope.Fill(image, health);

            Assert.AreEqual(3, scope.Count);

            scope.Clear();

            Assert.AreEqual(0, scope.Count);

            Assert.AreEqual(0, volume.SubscriberCount);

            Assert.AreEqual(0, health.SubscriberCount);

            health.Value = 0.1f;

            slider.value = 0.9f;

            Assert.AreEqual(1f, image.fillAmount);

            Assert.AreEqual(0.5f, volume.Value);
        }

        [Test]
        public void Rebind_RetargetsTheWidgetToTheNewObservable()
        {
            var scope = new BindingScope();

            var slider = Create<Slider>();

            var first = new Observable<float>();

            var second = new Observable<float>();

            scope.Value(slider, first);

            scope.Clear();

            scope.Value(slider, second);

            slider.value = 0.8f;

            Assert.AreEqual(0f, first.Value);

            Assert.AreEqual(0.8f, second.Value);
        }

        [Test]
        public void Rebind_InADifferentOrder_KeepsEachWidgetOnItsOwnObservable()
        {
            var scope = new BindingScope();

            var music = Create<Slider>();

            var effects = Create<Slider>();

            scope.Value(music, new Observable<float>());

            scope.Value(effects, new Observable<float>());

            scope.Clear();

            var effectsVolume = new Observable<float>();

            var musicVolume = new Observable<float>();

            scope.Value(effects, effectsVolume);

            scope.Value(music, musicVolume);

            music.value = 0.3f;

            effects.value = 0.6f;

            Assert.AreEqual(0.3f, musicVolume.Value);

            Assert.AreEqual(0.6f, effectsVolume.Value);
        }

        [Test]
        public void TheSameWidgetBoundTwice_WritesToBothObservables()
        {
            var scope = new BindingScope();

            var slider = Create<Slider>();

            var first = new Observable<float>();

            var second = new Observable<float>();

            scope.Value(slider, first);

            scope.Value(slider, second);

            slider.value = 0.4f;

            Assert.AreEqual(0.4f, first.Value);

            Assert.AreEqual(0.4f, second.Value);
        }

        [Test]
        public void Dispose_RemovesTheViewListeners()
        {
            var scope = new BindingScope();

            var slider = Create<Slider>();

            var button = Create<Button>();

            var volume = new Observable<float>(0.5f);

            var clicks = 0;

            var command = new Command(() => clicks++);

            scope.Value(slider, volume);

            scope.Click(button, command);

            scope.Dispose();

            slider.value = 0.9f;

            button.onClick.Invoke();

            Assert.AreEqual(0.5f, volume.Value);

            Assert.AreEqual(0, clicks);
        }

        [Test]
        public void TwoWay_DoesNotEcho()
        {
            var scope = new BindingScope();

            var slider = Create<Slider>();

            var volume = new Observable<float>();

            var viewEvents = 0;

            slider.onValueChanged.AddListener(_ => viewEvents++);

            scope.Value(slider, volume);

            volume.Value = 0.7f;

            Assert.AreEqual(0.7f, slider.value);

            Assert.AreEqual(0, viewEvents, "ViewModel → View must not raise the widget's event.");

            slider.value = 0.2f;

            Assert.AreEqual(0.2f, volume.Value);

            Assert.AreEqual(1, viewEvents);
        }

        [Test]
        public void ReadOnlySource_IsOneWay()
        {
            var scope = new BindingScope();

            var slider = Create<Slider>();

            var health = new Observable<float>(0.3f);

            scope.Value(slider, health.AsReadOnly());

            Assert.AreEqual(0.3f, slider.value);

            slider.value = 0.9f;

            Assert.AreEqual(0.3f, health.Value);
        }
    }
}
