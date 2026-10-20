using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ModernWestern.UI.ObserverVM.Tests;

namespace ModernWestern.UI.ObserverVM.EditorTools.Tests
{
    public class ViewBindingEditorTests
    {
        private readonly List<GameObject> _created = new();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            foreach (var gameObject in _created.Where(gameObject => gameObject != null))
            {
                Object.DestroyImmediate(gameObject);
            }

            _created.Clear();
        }

        [Test]
        public void Sources_ListObservablesAndCommands_Only()
        {
            var sources = ViewBindingSource.Of(typeof(TestViewModel));

            var health = sources.Single(source => source.Name == "Health");

            Assert.AreEqual(typeof(float), health.ValueType);

            Assert.IsTrue(health.Writable);

            Assert.IsTrue(sources.Single(source => source.Name == "Respawn").IsCommand);

            Assert.IsFalse(sources.Any(source => source.Name == "Respawns"), "A plain int cannot be bound.");

            Assert.AreEqual("Health (float, writable)", health.Describe());
        }

        [Test]
        public void Binders_AreFilteredByTargetAndValueType()
        {
            var health = ViewBindingSource.Find(typeof(TestViewModel), "Health");

            var forImage = ViewBinders.For(typeof(Image), health).Select(binder => binder.Id).ToArray();

            CollectionAssert.Contains(forImage, "Fill");

            CollectionAssert.DoesNotContain(forImage, "Text");

            var forSlider = ViewBinders.For(typeof(Slider), health).Single();

            Assert.AreEqual("Value", forSlider.Id);

            Assert.IsTrue(forSlider.Has(ViewBinderFeatures.TwoWay));

            var visible = ViewBindingSource.Find(typeof(TestViewModel), "Visible");

            Assert.IsTrue(ViewBinders.For(typeof(GameObject), visible).Single().Has(ViewBinderFeatures.Invert));
        }

        [Test]
        public void Problem_IsNullForAWorkingBinding()
        {
            var image = Create<Image>();

            Assert.IsNull(ViewBindingValidation.Problem(typeof(TestViewModel), new ViewBinding("Health", image, "Fill")));
        }

        [TestCase("Stamina", "Fill", "renamed")]
        [TestCase("Respawns", "Fill", "only Observable")]
        [TestCase("Health", "Text", "has no \"Text\"")]
        [TestCase("", "Fill", "pick a ViewModel member")]
        [TestCase("Health", "", "pick what to bind")]
        public void Problem_ExplainsWhatIsWrong(string source, string property, string expected)
        {
            var image = Create<Image>();

            StringAssert.Contains(expected, ViewBindingValidation.Problem(typeof(TestViewModel), new ViewBinding(source, image, property)));
        }

        [Test]
        public void Problem_ReportsAMissingTarget()
        {
            StringAssert.Contains("pick the widget", ViewBindingValidation.Problem(typeof(TestViewModel), new ViewBinding("Health", null, "Fill")));
        }

        [Test]
        public void Validator_ReportsBrokenViewBindings()
        {
            var image = Create<Image>();

            var widget = Create<ViewBoundWidget>();

            widget.ViewBindingArray = new[] { new ViewBinding("Health", image, "Fill"), new ViewBinding("Stamina", image, "Fill") };

            var issues = new List<BindWidgetIssue>();

            BindWidgetValidator.ValidateHierarchy(widget.gameObject, "test", issues);

            Assert.AreEqual(1, issues.Count);

            Assert.IsNull(issues[0].Field);

            Assert.AreEqual("Stamina", issues[0].Binding.Source);

            StringAssert.Contains("view binding \"Stamina", issues[0].Message);
        }

        [Test]
        public void LinkXml_PreservesTheViewModelOfEveryWidget()
        {
            var xml = ViewModelLinkXml.Build();

            StringAssert.Contains("fullname=\"ModernWestern.UI.ObserverVM.Tests.TestViewModel\" preserve=\"all\"", xml);

            StringAssert.Contains("fullname=\"ModernWestern.UI.ObserverVM.Tests.TestItem\" preserve=\"all\"", xml);

            StringAssert.Contains("ignoreIfMissing=\"1\"", xml);
        }

        private T Create<T>() where T : Component
        {
            var gameObject = new GameObject(typeof(T).Name, typeof(RectTransform));

            _created.Add(gameObject);

            return gameObject.AddComponent<T>();
        }
    }
}
