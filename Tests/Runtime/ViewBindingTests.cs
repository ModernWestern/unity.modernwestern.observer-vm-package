using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class ViewBindingTests : UiTestBase
    {
        private ViewBoundWidget Widget(params ViewBinding[] bindings)
        {
            var widget = Create<ViewBoundWidget>();

            widget.ViewBindingArray = bindings;

            return widget;
        }

        [Test]
        public void OneWay_FollowsTheViewModel()
        {
            var image = Create<Image>();

            var widget = Widget(new ViewBinding("Health", image, "Fill"));

            var viewModel = new TestViewModel();

            viewModel.Health.Value = 0.3f;

            widget.SetViewModel(viewModel);

            Assert.AreEqual(0.3f, image.fillAmount);

            viewModel.Health.Value = 0.6f;

            Assert.AreEqual(0.6f, image.fillAmount);
        }

        [Test]
        public void TwoWay_WritesUserInputBack()
        {
            var slider = Create<Slider>();

            var viewModel = new TestViewModel();

            Widget(new ViewBinding("Volume", slider, "Value")).SetViewModel(viewModel);

            Assert.AreEqual(0.5f, slider.value);

            slider.value = 0.9f;

            Assert.AreEqual(0.9f, viewModel.Volume.Value);

            viewModel.Volume.Value = 0.2f;

            Assert.AreEqual(0.2f, slider.value);
        }

        [Test]
        public void Invert_UsesTheOppositeValue()
        {
            var panel = CreateObject("Panel");

            var viewModel = new TestViewModel();

            Widget(new ViewBinding("Visible", panel, "Active", invert: true)).SetViewModel(viewModel);

            Assert.IsFalse(panel.activeSelf);

            viewModel.Visible.Value = false;

            Assert.IsTrue(panel.activeSelf);
        }

        [Test]
        public void Active_OnAComponent_TogglesItsGameObject()
        {
            var image = Create<Image>();

            var viewModel = new TestViewModel();

            Widget(new ViewBinding("Visible", image, "Active")).SetViewModel(viewModel);

            viewModel.Visible.Value = false;

            Assert.IsFalse(image.gameObject.activeSelf);
        }

        [Test]
        public void Command_ClickExecutes_AndFollowsCanExecute()
        {
            var button = Create<Button>();

            var viewModel = new TestViewModel();

            Widget(new ViewBinding("Respawn", button, "Click")).SetViewModel(viewModel);

            button.onClick.Invoke();

            Assert.AreEqual(1, viewModel.Respawns);

            viewModel.Respawn.CanExecute.Value = false;

            Assert.IsFalse(button.interactable);
        }

        [Test]
        public void PropertyMembers_Work()
        {
            var label = Create<Text>();

            var row = Create<ViewBoundRow>();

            row.ViewBindingArray = new[] { new ViewBinding("Name", label, "Text") };

            var item = new TestItem("Potion");

            row.SetViewModel(item);

            Assert.AreEqual("Potion", label.text);

            item.Name.Value = "Elixir";

            Assert.AreEqual("Elixir", label.text);
        }

        [Test]
        public void ViewBindings_RunAfterTheCodeBindings()
        {
            var widget = CreateWidget();

            var copy = Create<Text>();

            widget.ViewBindingArray = new[] { new ViewBinding("Nickname", copy, "Text") };

            var viewModel = new TestViewModel();

            viewModel.Nickname.Value = "Lobsang";

            widget.SetViewModel(viewModel);

            Assert.AreEqual("Lobsang", widget.Nickname.text, "Code binding from OnBind.");

            Assert.AreEqual("Lobsang", copy.text, "View binding from the Inspector.");
        }

        [Test]
        public void ABrokenBinding_IsSkippedWithAWarning_AndTheOthersStillWork()
        {
            var image = Create<Image>();

            var widget = Widget(new ViewBinding("Stamina", image, "Fill"), new ViewBinding("Health", image, "Fill"));

            // The hint differs: "renamed?" in the Editor, "code stripping" in players.
            LogAssert.Expect(LogType.Warning, new Regex("\"Stamina\""));

            var viewModel = new TestViewModel();

            viewModel.Health.Value = 0.4f;

            widget.SetViewModel(viewModel);

            Assert.AreEqual(0.4f, image.fillAmount);

            // Reported once, not on every bind.
            widget.SetViewModel(new TestViewModel());
        }

        [Test]
        public void Rebinding_MovesEveryBindingToTheNewViewModel()
        {
            var image = Create<Image>();

            var button = Create<Button>();

            var widget = Widget(new ViewBinding("Health", image, "Fill"), new ViewBinding("Respawn", button, "Click"));

            var first = new TestViewModel();

            var second = new TestViewModel();

            widget.SetViewModel(first);

            widget.SetViewModel(second);

            Assert.AreEqual(0, first.Health.SubscriberCount);

            first.Health.Value = 0.1f;

            second.Health.Value = 0.7f;

            Assert.AreEqual(0.7f, image.fillAmount);

            button.onClick.Invoke();

            Assert.AreEqual(0, first.Respawns);

            Assert.AreEqual(1, second.Respawns);
        }

#if OBSERVERVM_TMP
        [Test]
        public void NumbersWithAFormat_OnTextMeshPro()
        {
            if (TMPro.TMP_Settings.instance == null)
            {
                Assert.Ignore("TMP Essential Resources are not imported.");
            }

            var label = Create<TMPro.TextMeshProUGUI>();

            var viewModel = new TestViewModel();

            viewModel.Quality.Value = 2;

            Widget(new ViewBinding("Quality", label, "Text", "Level {0}")).SetViewModel(viewModel);

            Assert.AreEqual("Level 2", label.text);
        }
#endif
    }
}
