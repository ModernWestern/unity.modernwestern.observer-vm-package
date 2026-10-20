using NUnit.Framework;
using UnityEngine;
#if OBSERVERVM_TMP && !UNITY_EDITOR
using TMPro;
#endif
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace ModernWestern.UI.ObserverVM.Tests
{
    /// <summary>
    /// The package promise: after a widget is bound once, nothing allocates. Not changing values, not user input,
    /// not unbinding, not re-binding the widget to a ViewModel it has never seen (pooled list items).
    /// </summary>
    public class ZeroGcTests : UiTestBase
    {
        private TestWidget _widget;

        private TestViewModel _viewModel;

        // Mono creates a string literal the first time a method that uses it runs; keep that out of the measured code.
        private string _nickname;

        [SetUp]
        public void BindAndWarmUp()
        {
            _nickname = "Player";

            _widget = CreateWidget();

            // The first bind of a widget reserves its memory; binding a second ViewModel also warms up every code path.
            _widget.SetViewModel(new TestViewModel());

            _viewModel = new TestViewModel();

            _widget.SetViewModel(_viewModel);

            SetViewModelValues_Internal(0.9f, Color.blue, 0.4f, true, 1, "Warm", false, false);

            SetViewModelValues_Internal(0.8f, Color.white, 0.6f, false, 0, "Up", true, true);

            _widget.Volume.value = 0.1f;

            _widget.Muted.isOn = true;

            _widget.Quality.value = 2;

            _widget.Respawn.onClick.Invoke();

            _widget.Muted.isOn = false;

            _widget.Quality.value = 0;
        }

        [Test]
        public void ChangingViewModelValues_DoesNotAllocate()
        {
            Assert.That(() => SetViewModelValues_Internal(0.5f, Color.red, 0.3f, true, 2, _nickname, false, false), Is.Not.AllocatingGCMemory());

            Assert.AreEqual(0.5f, _widget.HealthBar.fillAmount);

            Assert.AreEqual(_nickname, _widget.Nickname.text);

            Assert.IsFalse(_widget.Respawn.interactable);
        }

        [Test]
        public void UserInput_DoesNotAllocate()
        {
            var respawns = _viewModel.Respawns;

            Assert.That(() =>
            {
                _widget.Volume.value = 0.9f;

                _widget.Muted.isOn = true;

                _widget.Quality.value = 1;

                _widget.Respawn.onClick.Invoke();
            }, Is.Not.AllocatingGCMemory());

            Assert.AreEqual(0.9f, _viewModel.Volume.Value);

            Assert.IsTrue(_viewModel.Muted.Value);

            Assert.AreEqual(1, _viewModel.Quality.Value);

            Assert.AreEqual(respawns + 1, _viewModel.Respawns);
        }

        [Test]
        public void Unbinding_DoesNotAllocate()
        {
            Assert.That(() => _widget.SetViewModel(null), Is.Not.AllocatingGCMemory());

            Assert.AreEqual(0, _viewModel.Health.SubscriberCount);
        }

        [Test]
        public void RebindingToANewViewModel_DoesNotAllocate()
        {
            var fresh = new TestViewModel();

            fresh.Health.Value = 0.33f;

            Assert.That(() => _widget.SetViewModel(fresh), Is.Not.AllocatingGCMemory());

            Assert.AreSame(fresh, _widget.ViewModel);

            Assert.AreEqual(0.33f, _widget.HealthBar.fillAmount);

            _widget.Volume.value = 0.05f;

            Assert.AreEqual(0.05f, fresh.Volume.Value);
        }

        [Test]
        public void ManyBindingsOnOneProperty_DoNotAllocate()
        {
            var scope = new BindingScope();

            var images = new UnityEngine.UI.Image[16];

            for (var i = 0; i < images.Length; i++)
            {
                images[i] = Create<UnityEngine.UI.Image>();
            }

            // Warm up: the first ViewModel with 16 bindings on one property reserves the memory, later ones reuse it.
            BindAll_Internal(scope, images, new Observable<float>());

            scope.Clear();

            var health = new Observable<float>();

            Assert.That(() => BindAll_Internal(scope, images, health), Is.Not.AllocatingGCMemory());

            Assert.That(() => { health.Value = 0.2f; }, Is.Not.AllocatingGCMemory());

            Assert.That(() => scope.Clear(), Is.Not.AllocatingGCMemory());

            Assert.AreEqual(0, health.SubscriberCount);
        }

        [Test]
        public void ListChanges_AndListRebinds_DoNotAllocate_OnceRowsArePooled()
        {
            var scope = new BindingScope();

            var container = CreateObject("Rows").transform;

            var template = Create<TestRowWidget>();

            template.transform.SetParent(container, false);

            template.Label = Create<UnityEngine.UI.Text>();

            template.Label.transform.SetParent(template.transform, false);

            var items = new ObservableList<TestItem>(8);

            var other = new ObservableList<TestItem>(8);

            var a = new TestItem("a");

            var b = new TestItem("b");

            var c = new TestItem("c");

            other.Add(new TestItem("x"));

            other.Add(new TestItem("y"));

            // Warm up: three rows get created, bound and pooled; the second list is bound once.
            scope.List(template, items);

            items.Add(a);

            items.Add(b);

            items.Add(c);

            items.Move(0, 2);

            items.Clear();

            scope.Clear();

            scope.List(template, other);

            scope.Clear();

            scope.List(template, items);

            Assert.That(() =>
            {
                items.Add(a);

                items.Add(b);

                items.Insert(0, c);

                items.Move(0, 2);

                items.RemoveAt(1);

                items[0] = b;

                items.Clear();
            }, Is.Not.AllocatingGCMemory());

            Assert.That(() =>
            {
                scope.Clear();

                scope.List(template, other);

                scope.Clear();

                scope.List(template, items);
            }, Is.Not.AllocatingGCMemory());

            scope.Dispose();
        }

        [Test]
        public void ViewBindings_RebindAndChanges_DoNotAllocate()
        {
            var image = Create<UnityEngine.UI.Image>();

            var slider = Create<UnityEngine.UI.Slider>();

            var button = Create<UnityEngine.UI.Button>();

            var label = Create<UnityEngine.UI.Text>();

            var panel = CreateObject("Panel");

            var widget = Create<ViewBoundWidget>();

            widget.ViewBindingArray = new[]
            {
                new ViewBinding("Health", image, "Fill"),
                new ViewBinding("Volume", slider, "Value"),
                new ViewBinding("Respawn", button, "Click"),
                new ViewBinding("Visible", panel, "Active", invert: true)
            };

            var row = Create<ViewBoundRow>();

            row.ViewBindingArray = new[] { new ViewBinding("Name", label, "Text") };

            // Warm up: the first binds resolve and cache the View Bindings (fields and properties).
            widget.SetViewModel(new TestViewModel());

            widget.SetViewModel(new TestViewModel());

            row.SetViewModel(new TestItem("a"));

            row.SetViewModel(new TestItem("b"));

            // UnityEvent builds its listener list the first time it fires after AddListener (one-time, inside Unity).
            slider.value = 0.1f;

            var fresh = new TestViewModel();

            var item = new TestItem("c");

            Assert.That(() =>
            {
                widget.SetViewModel(fresh);

                row.SetViewModel(item);
            }, Is.Not.AllocatingGCMemory());

            Assert.That(() =>
            {
                fresh.Health.Value = 0.25f;

                fresh.Visible.Value = false;

                slider.value = 0.75f;
            }, Is.Not.AllocatingGCMemory());

            Assert.AreEqual(0.25f, image.fillAmount);

            Assert.IsTrue(panel.activeSelf);

            Assert.AreEqual(0.75f, fresh.Volume.Value);
        }

#if OBSERVERVM_TMP
        [Test]
        public void TmpNumbers_DoNotAllocate()
        {
#if UNITY_EDITOR
            Assert.Ignore("TextMeshPro allocates inside the Editor (TMP_Text.SetText keeps an Inspector copy under #if UNITY_EDITOR). This test runs in players.");
#else
            if (TMP_Settings.instance == null)
            {
                Assert.Ignore("TMP Essential Resources are not imported.");
            }

            var label = Create<TextMeshProUGUI>();

            var scope = new BindingScope();

            var score = new Observable<int>();

            scope.Text(label, score, "Score {0}");

            score.Value = 1;

            Assert.That(() =>
            {
                score.Value = 2;

                score.Value = 3;
            }, Is.Not.AllocatingGCMemory());

            Assert.AreEqual("Score 3", label.text);

            scope.Dispose();
#endif
        }
#endif

#region Internal

        private static void BindAll_Internal(BindingScope scope, UnityEngine.UI.Image[] images, Observable<float> source)
        {
            foreach (var image in images)
            {
                scope.Fill(image, source);
            }
        }

        private void SetViewModelValues_Internal(float health, Color tint, float volume, bool muted, int quality, string nickname, bool visible, bool canRespawn)
        {
            _viewModel.Health.Value = health;

            _viewModel.Tint.Value = tint;

            _viewModel.Volume.Value = volume;

            _viewModel.Muted.Value = muted;

            _viewModel.Quality.Value = quality;

            _viewModel.Nickname.Value = nickname;

            _viewModel.Visible.Value = visible;

            _viewModel.Respawn.CanExecute.Value = canRespawn;
        }

#endregion
    }
}
