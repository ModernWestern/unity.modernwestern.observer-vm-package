using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class ObservableTests
    {
        private sealed class Probe<T>
        {
            public int Calls;

            public T Last;

            public void Record(T value)
            {
                Calls++;

                Last = value;
            }
        }

        private sealed class Saboteur
        {
            public Subscription Victim;
        }

        [Test]
        public void Subscribe_ReceivesTheCurrentValueImmediately()
        {
            var observable = new Observable<int>(5);

            var probe = new Probe<int>();

            observable.Subscribe(probe, static (p, value) => p.Record(value));

            Assert.AreEqual(1, probe.Calls);

            Assert.AreEqual(5, probe.Last);
        }

        [Test]
        public void Subscribe_WithoutNotifyNow_WaitsForTheNextChange()
        {
            var observable = new Observable<int>(5);

            var probe = new Probe<int>();

            observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            Assert.AreEqual(0, probe.Calls);

            observable.Value = 6;

            Assert.AreEqual(1, probe.Calls);

            Assert.AreEqual(6, probe.Last);
        }

        [Test]
        public void SettingTheSameValue_DoesNotNotify()
        {
            var observable = new Observable<float>(1f);

            var probe = new Probe<float>();

            observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            observable.Value = 1f;

            Assert.AreEqual(0, probe.Calls);
        }

        [Test]
        public void SetValueWithoutNotify_ChangesTheValueSilently()
        {
            var observable = new Observable<int>();

            var probe = new Probe<int>();

            observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            observable.SetValueWithoutNotify(3);

            Assert.AreEqual(3, observable.Value);

            Assert.AreEqual(0, probe.Calls);

            observable.Notify();

            Assert.AreEqual(1, probe.Calls);

            Assert.AreEqual(3, probe.Last);
        }

        [Test]
        public void Dispose_StopsNotifications_AndCanBeCalledTwice()
        {
            var observable = new Observable<int>();

            var probe = new Probe<int>();

            var subscription = observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            Assert.IsTrue(subscription.IsActive);

            subscription.Dispose();

            subscription.Dispose();

            observable.Value = 1;

            Assert.IsFalse(subscription.IsActive);

            Assert.AreEqual(0, probe.Calls);

            Assert.AreEqual(0, observable.SubscriberCount);
        }

        [Test]
        public void StaleSubscription_DoesNotRemoveTheSubscriberThatReusedItsSlot()
        {
            var observable = new Observable<int>();

            var first = new Probe<int>();

            var second = new Probe<int>();

            var stale = observable.Subscribe(first, static (p, value) => p.Record(value), notifyNow: false);

            stale.Dispose();

            var current = observable.Subscribe(second, static (p, value) => p.Record(value), notifyNow: false);

            stale.Dispose();

            observable.Value = 1;

            Assert.IsTrue(current.IsActive);

            Assert.AreEqual(1, second.Calls);

            Assert.AreEqual(0, first.Calls);
        }

        [Test]
        public void ManySubscribers_AreAllNotified_AndCounted()
        {
            var observable = new Observable<int>();

            var probes = new Probe<int>[10];

            var subscriptions = new Subscription[probes.Length];

            for (var i = 0; i < probes.Length; i++)
            {
                probes[i] = new Probe<int>();

                subscriptions[i] = observable.Subscribe(probes[i], static (p, value) => p.Record(value), notifyNow: false);
            }

            subscriptions[3].Dispose();

            subscriptions[7].Dispose();

            observable.Value = 42;

            Assert.AreEqual(8, observable.SubscriberCount);

            for (var i = 0; i < probes.Length; i++)
            {
                Assert.AreEqual(i is 3 or 7 ? 0 : 1, probes[i].Calls, $"probe {i}");
            }
        }

        [Test]
        public void UnsubscribingAnotherBindingDuringANotification_IsSafe()
        {
            var observable = new Observable<int>();

            var saboteur = new Saboteur();

            var victim = new Probe<int>();

            observable.Subscribe(saboteur, static (s, _) => s.Victim.Dispose(), notifyNow: false);

            saboteur.Victim = observable.Subscribe(victim, static (p, value) => p.Record(value), notifyNow: false);

            observable.Value = 1;

            Assert.AreEqual(0, victim.Calls);

            Assert.AreEqual(1, observable.SubscriberCount);
        }

        [Test]
        public void ChangingTheValueInsideANotification_TheLatestValueWins()
        {
            var observable = new Observable<int>();

            var probe = new Probe<int>();

            observable.Subscribe(observable, static (self, value) =>
            {
                if (value > 10)
                {
                    self.Value = 10;
                }
            }, notifyNow: false);

            observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            observable.Value = 50;

            Assert.AreEqual(10, observable.Value);

            Assert.AreEqual(10, probe.Last);

            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void AnExceptionInABinding_IsLogged_AndTheOthersStillRun()
        {
            var observable = new Observable<int>();

            var probe = new Probe<int>();

            observable.Subscribe("state", static (_, _) => throw new InvalidOperationException("boom"), notifyNow: false);

            observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            observable.Value = 1;

            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void CustomComparer_DecidesWhatCountsAsAChange()
        {
            var observable = new Observable<string>("hola", StringComparer.OrdinalIgnoreCase);

            var probe = new Probe<string>();

            observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            observable.Value = "HOLA";

            Assert.AreEqual(0, probe.Calls);

            observable.Value = "adiós";

            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void AfterEveryoneUnsubscribes_NewSubscribersWork_AndOldHandlesStayStale()
        {
            var observable = new Observable<int>();

            var probes = new Probe<int>[6];

            var stale = new Subscription[probes.Length];

            for (var i = 0; i < probes.Length; i++)
            {
                probes[i] = new Probe<int>();

                stale[i] = observable.Subscribe(probes[i], static (p, value) => p.Record(value), notifyNow: false);
            }

            foreach (var subscription in stale)
            {
                subscription.Dispose();
            }

            var fresh = new Probe<int>[3];

            for (var i = 0; i < fresh.Length; i++)
            {
                fresh[i] = new Probe<int>();

                observable.Subscribe(fresh[i], static (p, value) => p.Record(value), notifyNow: false);
            }

            foreach (var subscription in stale)
            {
                subscription.Dispose();
            }

            observable.Value = 9;

            Assert.AreEqual(3, observable.SubscriberCount);

            foreach (var probe in fresh)
            {
                Assert.AreEqual(1, probe.Calls);
            }

            foreach (var probe in probes)
            {
                Assert.AreEqual(0, probe.Calls);
            }
        }

        [Test]
        public void UnsubscribingEveryoneDuringANotification_IsSafe()
        {
            var observable = new Observable<int>();

            var saboteurs = new Saboteur[5];

            for (var i = 0; i < saboteurs.Length; i++)
            {
                saboteurs[i] = new Saboteur();

                saboteurs[i].Victim = observable.Subscribe(saboteurs[i], static (s, _) => s.Victim.Dispose(), notifyNow: false);
            }

            observable.Value = 1;

            Assert.AreEqual(0, observable.SubscriberCount);

            var probe = new Probe<int>();

            observable.Subscribe(probe, static (p, value) => p.Record(value), notifyNow: false);

            observable.Value = 2;

            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void SubscribeWithArgument_PassesTheArgument()
        {
            var observable = new Observable<int>(7);

            var probe = new Probe<string>();

            observable.Subscribe(probe, "value:", static (p, prefix, value) => p.Record(prefix + value));

            Assert.AreEqual("value:7", probe.Last);
        }
    }
}
