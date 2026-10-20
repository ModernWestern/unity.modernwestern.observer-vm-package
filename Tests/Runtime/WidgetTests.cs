using NUnit.Framework;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class WidgetTests : UiTestBase
    {
        [Test]
        public void SetViewModel_PushesEveryValueToTheWidgets()
        {
            var widget = CreateWidget();

            var viewModel = new TestViewModel();

            viewModel.Health.Value = 0.25f;

            viewModel.Tint.Value = Color.red;

            viewModel.Volume.Value = 0.75f;

            viewModel.Muted.Value = true;

            viewModel.Quality.Value = 2;

            viewModel.Nickname.Value = "Lobsang";

            viewModel.Visible.Value = false;

            widget.SetViewModel(viewModel);

            Assert.AreSame(viewModel, widget.ViewModel);

            Assert.AreEqual(0.25f, widget.HealthBar.fillAmount);

            Assert.AreEqual(Color.red, widget.HealthBar.color);

            Assert.AreEqual(0.75f, widget.Volume.value);

            Assert.IsTrue(widget.Muted.isOn);

            Assert.AreEqual(2, widget.Quality.value);

            Assert.AreEqual("Lobsang", widget.Nickname.text);

            Assert.IsFalse(widget.Panel.activeSelf);

            Assert.AreEqual(0.25f, widget.LastHealth);
        }

        [Test]
        public void ViewModelChanges_ReachTheWidgets()
        {
            var widget = CreateWidget();

            var viewModel = new TestViewModel();

            widget.SetViewModel(viewModel);

            viewModel.Health.Value = 0.4f;

            viewModel.Volume.Value = 0.1f;

            viewModel.Nickname.Value = "Player";

            Assert.AreEqual(0.4f, widget.HealthBar.fillAmount);

            Assert.AreEqual(0.4f, widget.LastHealth);

            Assert.AreEqual(0.1f, widget.Volume.value);

            Assert.AreEqual("Player", widget.Nickname.text);
        }

        [Test]
        public void UserInput_ReachesTheViewModel()
        {
            var widget = CreateWidget();

            var viewModel = new TestViewModel();

            widget.SetViewModel(viewModel);

            widget.Volume.value = 0.9f;

            widget.Muted.isOn = true;

            widget.Quality.value = 1;

            widget.Respawn.onClick.Invoke();

            Assert.AreEqual(0.9f, viewModel.Volume.Value);

            Assert.IsTrue(viewModel.Muted.Value);

            Assert.AreEqual(1, viewModel.Quality.Value);

            Assert.AreEqual(1, viewModel.Respawns);
        }

        [Test]
        public void SwitchingViewModels_DisconnectsThePreviousOne()
        {
            var widget = CreateWidget();

            var first = new TestViewModel();

            var second = new TestViewModel();

            widget.SetViewModel(first);

            widget.SetViewModel(second);

            Assert.AreEqual(1, widget.UnbindCount);

            Assert.AreSame(first, widget.LastUnbound);

            Assert.AreEqual(0, first.Health.SubscriberCount);

            first.Health.Value = 0.1f;

            Assert.AreEqual(1f, widget.HealthBar.fillAmount);

            widget.Volume.value = 0.2f;

            widget.Respawn.onClick.Invoke();

            Assert.AreEqual(0.5f, first.Volume.Value);

            Assert.AreEqual(0.2f, second.Volume.Value);

            Assert.AreEqual(0, first.Respawns);

            Assert.AreEqual(1, second.Respawns);
        }

        [Test]
        public void SettingTheSameViewModel_DoesNotRebind()
        {
            var widget = CreateWidget();

            var viewModel = new TestViewModel();

            widget.SetViewModel(viewModel);

            widget.SetViewModel(viewModel);

            Assert.AreEqual(1, widget.BindCount);

            Assert.AreEqual(0, widget.UnbindCount);
        }

        [Test]
        public void SettingNull_Unbinds()
        {
            var widget = CreateWidget();

            var viewModel = new TestViewModel();

            widget.SetViewModel(viewModel);

            widget.SetViewModel(null);

            Assert.IsNull(widget.ViewModel);

            Assert.AreEqual(0, viewModel.Health.SubscriberCount);

            widget.Volume.value = 0.9f;

            Assert.AreEqual(0.5f, viewModel.Volume.Value);
        }

        [Test]
        public void DestroyingTheWidget_Unbinds()
        {
            var widget = CreateWidget();

            var viewModel = new TestViewModel();

            widget.SetViewModel(viewModel);

            var slider = widget.Volume;

            Object.DestroyImmediate(widget.gameObject);

            Assert.AreEqual(0, viewModel.Health.SubscriberCount);

            Assert.AreEqual(0, viewModel.Respawn.CanExecute.SubscriberCount);

            slider.value = 0.9f;

            Assert.AreEqual(0.5f, viewModel.Volume.Value);
        }

        [Test]
        public void CommandCanExecute_DrivesTheButton()
        {
            var widget = CreateWidget();

            var viewModel = new TestViewModel();

            widget.SetViewModel(viewModel);

            viewModel.Respawn.CanExecute.Value = false;

            Assert.IsFalse(widget.Respawn.interactable);

            widget.Respawn.onClick.Invoke();

            Assert.AreEqual(0, viewModel.Respawns);

            viewModel.Respawn.CanExecute.Value = true;

            Assert.IsTrue(widget.Respawn.interactable);
        }
    }
}
