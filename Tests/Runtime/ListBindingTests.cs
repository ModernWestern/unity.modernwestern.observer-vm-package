using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class ListBindingTests : UiTestBase
    {
        private BindingScope _scope;

        private TestRowWidget _template;

        [SetUp]
        public void CreateTemplate()
        {
            _scope = new BindingScope();

            var container = CreateObject("Rows").transform;

            _template = Create<TestRowWidget>();

            _template.name = "Row";

            _template.transform.SetParent(container, false);

            _template.Label = Create<Text>();

            _template.Label.transform.SetParent(_template.transform, false);
        }

        [TearDown]
        public void DisposeScope()
        {
            _scope.Dispose();
        }

        [Test]
        public void Bind_CreatesOneRowPerItem_InOrder_AndHidesTheTemplate()
        {
            var items = Items("a", "b", "c");

            _scope.List(_template, items);

            Assert.IsFalse(_template.gameObject.activeSelf);

            AssertRows(items);
        }

        [Test]
        public void Rows_FollowEveryChangeOfTheList()
        {
            var items = Items("a", "b", "c");

            _scope.List(_template, items);

            items.Insert(1, new TestItem("x"));

            AssertRows(items);

            items.RemoveAt(0);

            AssertRows(items);

            items[1] = new TestItem("y");

            AssertRows(items);

            items.Move(0, 2);

            AssertRows(items);

            items.Clear();

            AssertRows(items);

            items.Add(new TestItem("z"));

            AssertRows(items);
        }

        [Test]
        public void ItemChanges_ReachTheirRow()
        {
            var items = Items("a", "b");

            _scope.List(_template, items);

            items[1].Name.Value = "B";

            Assert.AreEqual("B", Rows()[1].Label.text);
        }

        [Test]
        public void RemovedRows_AreReused()
        {
            var items = Items("a", "b", "c");

            _scope.List(_template, items);

            var childCount = _template.transform.parent.childCount;

            items.RemoveAt(0);

            items.Add(new TestItem("d"));

            Assert.AreEqual(childCount, _template.transform.parent.childCount, "No new row should be created.");

            AssertRows(items);
        }

        [Test]
        public void Rebinding_ToAnotherList_ReusesTheRows()
        {
            var first = Items("a", "b", "c");

            _scope.List(_template, first);

            var childCount = _template.transform.parent.childCount;

            _scope.Clear();

            Assert.AreEqual(0, Rows().Count);

            Assert.AreEqual(0, first.SubscriberCount);

            var second = Items("x", "y");

            _scope.List(_template, second);

            Assert.AreEqual(childCount, _template.transform.parent.childCount);

            AssertRows(second);

            first.Add(new TestItem("ignored"));

            AssertRows(second);
        }

        [Test]
        public void UnboundRows_ReleaseTheirItems()
        {
            var items = Items("a");

            _scope.List(_template, items);

            var item = items[0];

            items.RemoveAt(0);

            Assert.AreEqual(0, item.Name.SubscriberCount);
        }

        private static ObservableList<TestItem> Items(params string[] names)
        {
            return new ObservableList<TestItem>(names.Select(name => new TestItem(name)));
        }

        private List<TestRowWidget> Rows()
        {
            return _template.transform.parent.Cast<Transform>()
                            .Where(child => child.gameObject.activeSelf)
                            .Select(child => child.GetComponent<TestRowWidget>())
                            .ToList();
        }

        private void AssertRows(ObservableList<TestItem> items)
        {
            var rows = Rows();

            Assert.AreEqual(items.Count, rows.Count, "One active row per item.");

            for (var i = 0; i < items.Count; i++)
            {
                Assert.AreSame(items[i], rows[i].ViewModel, $"Row {i}");

                Assert.AreEqual(items[i].Name.Value, rows[i].Label.text, $"Row {i} text");
            }
        }
    }
}
