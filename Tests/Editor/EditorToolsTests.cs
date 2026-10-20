using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ModernWestern.UI.ObserverVM.Tests;

namespace ModernWestern.UI.ObserverVM.EditorTools.Tests
{
    public class EditorToolsTests
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
        public void BindWidgetFields_FindsPrivateInheritedAndOptionalFields()
        {
            var fields = BindWidgetFields.Of(typeof(TestWidget));

            Assert.AreEqual(7, fields.Count);

            Assert.IsTrue(fields.Single(field => field.Name == "Panel").Optional);

            Assert.AreEqual("Health Bar", fields.Single(field => field.Name == "HealthBar").DisplayName);
        }

        [TestCase("Health Bar", "HealthBar", true)]
        [TestCase("health_bar", "HealthBar", true)]
        [TestCase("Health Bar Image", "HealthBar", true)]
        [TestCase("Volume Slider", "Volume", true)]
        [TestCase("Respawn Button", "Respawn", true)]
        [TestCase("Respawn Btn", "Respawn", true)]
        [TestCase("Health", "HealthBar", false)]
        [TestCase("Health Bar Background", "HealthBar", false)]
        [TestCase("Volume", "Muted", false)]
        public void Matches_FollowsUnrealStyleNames(string objectName, string fieldName, bool expected)
        {
            var field = BindWidgetFields.Of(typeof(TestWidget)).Single(each => each.Name == fieldName);

            Assert.AreEqual(expected, BindWidgetAutoAssigner.Matches(objectName, field));
        }

        [Test]
        public void AutoAssign_FillsEveryFieldFromMatchingChildren()
        {
            var widget = CreateWidgetWithChildren_Internal();

            var report = BindWidgetAutoAssigner.Assign(widget);

            Assert.AreEqual(7, report.Count(AutoAssignOutcome.Assigned), report.Summary);

            Assert.AreEqual("Health Bar", widget.HealthBar.name);

            Assert.AreEqual("Volume Slider", widget.Volume.name);

            Assert.AreEqual("Respawn Button", widget.Respawn.name);

            Assert.AreEqual("Panel", widget.Panel.name);
        }

        [Test]
        public void AutoAssign_NeverOverwritesAFieldThatIsAlreadySet()
        {
            var widget = CreateWidgetWithChildren_Internal();

            var custom = Child_Internal<Image>(widget.transform, "Something Else");

            widget.HealthBar = custom;

            var report = BindWidgetAutoAssigner.Assign(widget);

            Assert.AreSame(custom, widget.HealthBar);

            Assert.AreEqual(1, report.Count(AutoAssignOutcome.AlreadySet));
        }

        [Test]
        public void AutoAssign_LeavesAmbiguousFieldsEmpty()
        {
            var widget = CreateWidgetWithChildren_Internal();

            Child_Internal<Button>(widget.transform, "Respawn");

            var report = BindWidgetAutoAssigner.Assign(widget);

            Assert.IsNull(widget.Respawn);

            Assert.AreEqual(2, report.Entries.Single(entry => entry.Field.Name == "Respawn").Matches.Count);

            StringAssert.Contains("Respawn (2 matches)", report.Summary);
        }

        [Test]
        public void AutoAssign_DoesNotLookInsideANestedWidget()
        {
            var widget = Root_Internal();

            var nested = Root_Internal();

            nested.transform.SetParent(widget.transform, false);

            Child_Internal<Button>(nested.transform, "Respawn Button");

            BindWidgetAutoAssigner.Assign(widget);

            Assert.IsNull(widget.Respawn);
        }

        [Test]
        public void AutoAssign_CanBeUndone()
        {
            var widget = CreateWidgetWithChildren_Internal();

            Undo.IncrementCurrentGroup();

            BindWidgetAutoAssigner.Assign(widget);

            Assert.IsNotNull(widget.HealthBar);

            Undo.PerformUndo();

            Assert.IsNull(widget.HealthBar);
        }

        [Test]
        public void Validator_ReportsOnlyRequiredEmptyFields()
        {
            var widget = Root_Internal();

            var issues = new List<BindWidgetIssue>();

            BindWidgetValidator.Validate(widget, "test", issues);

            Assert.AreEqual(6, issues.Count, "7 fields, Panel is optional.");

            Assert.IsFalse(issues.Any(issue => issue.Field.Name == "Panel"));

            StringAssert.Contains("\"Health Bar\" is empty (Image needed)", issues.First(issue => issue.Field.Name == "HealthBar").Message);
        }

        [Test]
        public void Validator_FindsNothingOnceEverythingIsAssigned()
        {
            var widget = CreateWidgetWithChildren_Internal();

            BindWidgetAutoAssigner.Assign(widget);

            var issues = new List<BindWidgetIssue>();

            BindWidgetValidator.ValidateHierarchy(widget.gameObject, "test", issues);

            Assert.AreEqual(0, issues.Count);
        }

        [TestCase("Inventory", "Inventory")]
        [TestCase("InventoryWidget.cs", "Inventory")]
        [TestCase("inventory view model", "Inventory")]
        [TestCase("Player HUD", "PlayerHUD")]
        [TestCase("3d menu", "_3dmenu")]
        [TestCase("Widget", "Widget")]
        public void CreateScripts_BaseNameIsAValidIdentifier(string input, string expected)
        {
            Assert.AreEqual(expected, CreateWidgetScripts.BaseName(input));
        }

#region Internal

        private TestWidget Root_Internal()
        {
            var gameObject = new GameObject("Widget", typeof(RectTransform));

            _created.Add(gameObject);

            return gameObject.AddComponent<TestWidget>();
        }

        private static T Child_Internal<T>(Transform parent, string name) where T : Component
        {
            var gameObject = new GameObject(name, typeof(RectTransform));

            gameObject.transform.SetParent(parent, false);

            return gameObject.AddComponent<T>();
        }

        private TestWidget CreateWidgetWithChildren_Internal()
        {
            var widget = Root_Internal();

            var card = new GameObject("Card", typeof(RectTransform)).transform;

            card.SetParent(widget.transform, false);

            Child_Internal<Image>(card, "Health Bar");

            Child_Internal<Slider>(card, "Volume Slider");

            Child_Internal<Toggle>(card, "Muted");

            Child_Internal<Dropdown>(card, "Quality Dropdown");

            Child_Internal<Text>(card, "Nickname Text");

            Child_Internal<Button>(card, "Respawn Button");

            new GameObject("Panel", typeof(RectTransform)).transform.SetParent(card, false);

            return widget;
        }

#endregion
    }
}
