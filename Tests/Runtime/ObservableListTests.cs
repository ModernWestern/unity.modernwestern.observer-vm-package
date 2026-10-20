using NUnit.Framework;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class ObservableListTests
    {
        private sealed class Log
        {
            public readonly List<ListChange<string>> Changes = new();
        }

        private static (ObservableList<string> list, Log log, Subscription subscription) Create(params string[] items)
        {
            var list = new ObservableList<string>(items);

            var log = new Log();

            var subscription = list.Subscribe(log, static (l, change) => l.Changes.Add(change));

            return (list, log, subscription);
        }

        [Test]
        public void Add_And_Insert_ReportTheIndex()
        {
            var (list, log, _) = Create("a");

            list.Add("c");

            list.Insert(1, "b");

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list);

            Assert.AreEqual(ListChangeKind.Add, log.Changes[0].Kind);

            Assert.AreEqual(1, log.Changes[0].Index);

            Assert.AreEqual(1, log.Changes[1].Index);

            Assert.AreEqual("b", log.Changes[1].Item);
        }

        [Test]
        public void Remove_ReportsTheRemovedItem()
        {
            var (list, log, _) = Create("a", "b", "c");

            Assert.IsTrue(list.Remove("b"));

            Assert.IsFalse(list.Remove("x"));

            Assert.AreEqual(1, log.Changes.Count);

            Assert.AreEqual(ListChangeKind.Remove, log.Changes[0].Kind);

            Assert.AreEqual(1, log.Changes[0].Index);

            Assert.AreEqual("b", log.Changes[0].OldItem);
        }

        [Test]
        public void Replace_ReportsOldAndNewItems()
        {
            var (list, log, _) = Create("a", "b");

            list[1] = "B";

            Assert.AreEqual(ListChangeKind.Replace, log.Changes[0].Kind);

            Assert.AreEqual("B", log.Changes[0].Item);

            Assert.AreEqual("b", log.Changes[0].OldItem);
        }

        [Test]
        public void Move_ReportsBothIndices()
        {
            var (list, log, _) = Create("a", "b", "c");

            list.Move(0, 2);

            CollectionAssert.AreEqual(new[] { "b", "c", "a" }, list);

            Assert.AreEqual(ListChangeKind.Move, log.Changes[0].Kind);

            Assert.AreEqual(2, log.Changes[0].Index);

            Assert.AreEqual(0, log.Changes[0].OldIndex);
        }

        [Test]
        public void Clear_ReportsAReset()
        {
            var (list, log, _) = Create("a", "b");

            list.Clear();

            Assert.AreEqual(0, list.Count);

            Assert.AreEqual(ListChangeKind.Reset, log.Changes[0].Kind);
        }

        [Test]
        public void Dispose_StopsNotifications()
        {
            var (list, log, subscription) = Create("a");

            Assert.AreEqual(1, list.SubscriberCount);

            subscription.Dispose();

            list.Add("b");

            Assert.AreEqual(0, log.Changes.Count);

            Assert.AreEqual(0, list.SubscriberCount);
        }

        [Test]
        public void EverySubscriberSeesEveryChange_EvenWhenOneChangesTheList()
        {
            var list = new ObservableList<string>();

            var first = new Log();

            var second = new Log();

            list.Subscribe(list, static (l, change) =>
            {
                if (change.Kind == ListChangeKind.Add && change.Item == "trigger")
                {
                    l.Add("nested");
                }
            });

            list.Subscribe(first, static (l, change) => l.Changes.Add(change));

            list.Subscribe(second, static (l, change) => l.Changes.Add(change));

            list.Add("trigger");

            Assert.AreEqual(2, first.Changes.Count);

            Assert.AreEqual(2, second.Changes.Count);

            CollectionAssert.AreEqual(new[] { "trigger", "nested" }, list);
        }
    }
}
