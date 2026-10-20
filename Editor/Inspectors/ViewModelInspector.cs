using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Live view of a ViewModel in Play mode: observable values (editable when writable), commands and binding counts.
    /// Editing a value goes through Observable.Value, so every bound widget reacts as if gameplay had changed it.
    /// </summary>
    internal static class ViewModelInspector
    {
        private enum Kind
        {
            Observable,
            ReadOnlyObservable,
            Command,
            Plain
        }

        private sealed class Member
        {
            public string Name;

            public Kind Kind;

            public Type Type;

            public Func<object, object> Get;
        }

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

        private static readonly Dictionary<Type, Member[]> Cache = new();

        public static void Draw(object viewModel, int bindingCount)
        {
            if (viewModel == null)
            {
                EditorGUILayout.LabelField("No ViewModel bound. Call SetViewModel(...) from your code to bind one.", ObserverVMStyles.MutedLabel);

                return;
            }

            EditorGUILayout.LabelField(viewModel.GetType().Name, $"{bindingCount} active binding(s)");

            var members = MembersOf_Internal(viewModel.GetType());

            if (members.Length == 0)
            {
                EditorGUILayout.LabelField("This ViewModel has no public Observable, Command or value to show.", ObserverVMStyles.MutedLabel);

                return;
            }

            foreach (var member in members)
            {
                object value;

                try
                {
                    value = member.Get(viewModel);
                }
                catch (Exception exception)
                {
                    EditorGUILayout.LabelField(member.Name, $"(error: {exception.GetBaseException().Message})");

                    continue;
                }

                switch (member.Kind)
                {
                    case Kind.Observable:
                    case Kind.ReadOnlyObservable:
                        DrawObservable_Internal(member, value);
                        break;
                    case Kind.Command:
                        DrawCommand_Internal(member.Name, value as Command);
                        break;
                    default:
                        EditorGUILayout.LabelField(member.Name, value?.ToString() ?? "null");
                        break;
                }
            }
        }

#region Internal

        private static void DrawObservable_Internal(Member member, object observable)
        {
            if (observable == null)
            {
                EditorGUILayout.LabelField(member.Name, "null");

                return;
            }

            var type = observable.GetType();

            var valueProperty = type.GetProperty("Value", Flags);

            var subscribers = type.GetProperty("SubscriberCount", Flags)?.GetValue(observable);

            var valueType = valueProperty.PropertyType;

            var current = valueProperty.GetValue(observable);

            var editable = member.Kind == Kind.Observable && valueProperty.CanWrite;

            var label = new GUIContent(member.Name, $"{(editable ? "Observable" : "Read-only")}<{valueType.Name}>, {subscribers} binding(s). {(editable ? "Edit it to push a new value to the widgets." : "Read-only: change it from the ViewModel.")}");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!editable))
                {
                    EditorGUI.BeginChangeCheck();

                    var edited = DrawValue_Internal(label, valueType, current);

                    if (EditorGUI.EndChangeCheck() && editable)
                    {
                        valueProperty.SetValue(observable, edited);
                    }
                }

                GUILayout.Label($"{subscribers}", ObserverVMStyles.MutedLabel, GUILayout.Width(24));
            }
        }

        private static void DrawCommand_Internal(string name, Command command)
        {
            if (command == null)
            {
                EditorGUILayout.LabelField(name, "null");

                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent(name, "Command. The button runs it like a click would."));

                GUILayout.Label(command.CanExecute.Value ? "Can execute" : "Cannot execute now", command.CanExecute.Value ? EditorStyles.label : ObserverVMStyles.MutedLabel);

                using (new EditorGUI.DisabledScope(!command.CanExecute.Value))
                {
                    if (GUILayout.Button("Execute", EditorStyles.miniButton, GUILayout.Width(64)))
                    {
                        command.Execute();
                    }
                }
            }
        }

        private static object DrawValue_Internal(GUIContent label, Type type, object value)
        {
            if (type == typeof(int)) return EditorGUILayout.IntField(label, (int)value);
            if (type == typeof(float)) return EditorGUILayout.FloatField(label, (float)value);
            if (type == typeof(double)) return EditorGUILayout.DoubleField(label, (double)value);
            if (type == typeof(long)) return EditorGUILayout.LongField(label, (long)value);
            if (type == typeof(bool)) return EditorGUILayout.Toggle(label, (bool)value);
            if (type == typeof(string)) return EditorGUILayout.TextField(label, (string)value ?? string.Empty);
            if (type == typeof(Color)) return EditorGUILayout.ColorField(label, (Color)value);
            if (type == typeof(Vector2)) return EditorGUILayout.Vector2Field(label, (Vector2)value);
            if (type == typeof(Vector3)) return EditorGUILayout.Vector3Field(label, (Vector3)value);
            if (type.IsEnum) return EditorGUILayout.EnumPopup(label, (Enum)value);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return EditorGUILayout.ObjectField(label, (UnityEngine.Object)value, type, true);

            EditorGUILayout.LabelField(label, new GUIContent(value?.ToString() ?? "null"));

            return value;
        }

        private static Member[] MembersOf_Internal(Type type)
        {
            if (Cache.TryGetValue(type, out var members))
            {
                return members;
            }

            var found = new List<Member>();

            foreach (var field in type.GetFields(Flags))
            {
                found.Add(new Member { Name = ObjectNames.NicifyVariableName(field.Name), Kind = KindOf_Internal(field.FieldType), Type = field.FieldType, Get = field.GetValue });
            }

            foreach (var property in type.GetProperties(Flags).Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
            {
                found.Add(new Member { Name = ObjectNames.NicifyVariableName(property.Name), Kind = KindOf_Internal(property.PropertyType), Type = property.PropertyType, Get = property.GetValue });
            }

            members = found.Where(member => member.Kind != Kind.Plain || IsSimple_Internal(member.Type)).ToArray();

            Cache[type] = members;

            return members;
        }

        private static bool IsSimple_Internal(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string);
        }

        private static Kind KindOf_Internal(Type type)
        {
            if (typeof(Command).IsAssignableFrom(type))
            {
                return Kind.Command;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Observable<>))
            {
                return Kind.Observable;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyObservable<>))
            {
                return Kind.ReadOnlyObservable;
            }

            return Kind.Plain;
        }

#endregion
    }
}
