using System;
using NUnit.Framework;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class ViewModelAndCommandTests : UiTestBase
    {
        private sealed class Derived : ViewModel
        {
            public readonly Observable<bool> IsRich = new();

            public int Disposals;

            public Derived(IReadOnlyObservable<int> gold)
            {
                Watch(gold, this, static (self, value) => self.IsRich.Value = value >= 100);
            }

            public void WatchAgain(IReadOnlyObservable<int> gold)
            {
                Watch(gold, this, static (_, _) => { });
            }

            protected override void OnDispose()
            {
                Disposals++;
            }
        }

        private sealed class Picks
        {
            public int Last = -1;

            public int Count;
        }

        [Test]
        public void Watch_FollowsTheSource_UntilDisposed()
        {
            var gold = new Observable<int>(150);

            var derived = new Derived(gold);

            Assert.IsTrue(derived.IsRich.Value);

            gold.Value = 10;

            Assert.IsFalse(derived.IsRich.Value);

            derived.Dispose();

            Assert.AreEqual(0, gold.SubscriberCount);

            gold.Value = 500;

            Assert.IsFalse(derived.IsRich.Value);
        }

        [Test]
        public void Dispose_RunsOnce_AndWatchingAfterwardsThrows()
        {
            var gold = new Observable<int>();

            var derived = new Derived(gold);

            derived.Dispose();

            derived.Dispose();

            Assert.AreEqual(1, derived.Disposals);

            Assert.IsTrue(derived.IsDisposed);

            Assert.Throws<ObjectDisposedException>(() => derived.WatchAgain(gold));
        }

        [Test]
        public void CommandWithParameter_ExecutesWithIt_OnlyWhenAllowed()
        {
            var picks = new Picks();

            var select = new Command<int>(index => picks.Last = index);

            select.Execute(3);

            Assert.AreEqual(3, picks.Last);

            select.CanExecute.Value = false;

            select.Execute(7);

            Assert.AreEqual(3, picks.Last);
        }

        [Test]
        public void ClickWithParameter_PassesTheParameter_AndFollowsCanExecute()
        {
            var scope = new BindingScope();

            var button = Create<Button>();

            var picks = new Picks();

            var select = new Command<int>(index => { picks.Last = index; picks.Count++; });

            scope.Click(button, select, 4);

            button.onClick.Invoke();

            Assert.AreEqual(4, picks.Last);

            select.CanExecute.Value = false;

            Assert.IsFalse(button.interactable);

            select.CanExecute.Value = true;

            scope.Clear();

            scope.Click(button, select, 9);

            button.onClick.Invoke();

            Assert.AreEqual(9, picks.Last);

            Assert.AreEqual(2, picks.Count, "The listener is reused, not added twice.");

            scope.Dispose();
        }

        [Test]
        public void Child_GetsTheViewModel_WhileTheScopeIsBound()
        {
            var scope = new BindingScope();

            var child = Create<TestRowWidget>();

            child.Label = Create<Text>();

            var item = new TestItem("a");

            scope.Child(child, item);

            Assert.AreSame(item, child.ViewModel);

            scope.Clear();

            Assert.IsNull(child.ViewModel);

            scope.Dispose();
        }

        [Test]
        public void Child_FollowsAnObservableViewModel()
        {
            var scope = new BindingScope();

            var child = Create<TestRowWidget>();

            child.Label = Create<Text>();

            var selected = new Observable<TestItem>(new TestItem("a"));

            scope.Child(child, selected);

            Assert.AreEqual("a", child.Label.text);

            selected.Value = new TestItem("b");

            Assert.AreEqual("b", child.Label.text);

            scope.Clear();

            Assert.IsNull(child.ViewModel);

            selected.Value = new TestItem("c");

            Assert.IsNull(child.ViewModel);

            scope.Dispose();
        }
    }
}
