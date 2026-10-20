using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.ProjectWindowCallback;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Assets > Create > ObserverVM > Widget + ViewModel: type a name ("Inventory") and get InventoryViewModel.cs and InventoryWidget.cs.
    /// </summary>
    public static class CreateWidgetScripts
    {
        private const string ViewModelTemplate =
@"using ModernWestern.UI.ObserverVM;

// The state and actions of #NAME#. It knows nothing about the UI.
public sealed class #NAME#ViewModel
{
    public #NAME#ViewModel()
    {
        Confirm = new Command(OnConfirm);
    }

    public Observable<string> Title { get; } = new(""#NAME#"");

    public Command Confirm { get; }

    private void OnConfirm()
    {
    }
}
";

        private const string WidgetTemplate =
@"using UnityEngine;
using UnityEngine.UI;
using ModernWestern.UI.ObserverVM;

// The view of #NAME#ViewModel. Name the children like the fields (e.g. ""Confirm Button"") and press Auto-assign by name.
public sealed class #NAME#Widget : Widget<#NAME#ViewModel>
{
    [SerializeField, BindWidget] private Button confirm;

    protected override void OnBind(#NAME#ViewModel viewModel)
    {
        Bind.Click(confirm, viewModel.Confirm);

        // With TextMeshPro: add [SerializeField, BindWidget] private TMPro.TMP_Text title; and Bind.Text(title, viewModel.Title);
    }
}
";

        [MenuItem("Assets/Create/ObserverVM/Widget + ViewModel", false, 81)]
        private static void Create()
        {
            var icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateAction>(), "NewWidget.cs", icon, null);
        }

        /// <summary>
        /// "Inventory Widget" → "Inventory": a valid C# identifier without a trailing "Widget" or "ViewModel".
        /// </summary>
        public static string BaseName(string input)
        {
            var name = new StringBuilder();

            foreach (var character in Path.GetFileNameWithoutExtension(input))
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                {
                    name.Append(name.Length == 0 && char.IsDigit(character) ? "_" + character : character.ToString());
                }
            }

            var result = name.ToString();

            foreach (var suffix in new[] { "ViewModel", "Widget" })
            {
                if (result.Length > suffix.Length && result.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(0, result.Length - suffix.Length);
                }
            }

            return result.Length == 0 ? "New" : char.ToUpperInvariant(result[0]) + result.Substring(1);
        }

        private sealed class CreateAction : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var folder = Path.GetDirectoryName(pathName) ?? "Assets";

                var name = BaseName(pathName);

                var viewModelPath = Write_Internal(folder, $"{name}ViewModel.cs", ViewModelTemplate.Replace("#NAME#", name));

                var widgetPath = Write_Internal(folder, $"{name}Widget.cs", WidgetTemplate.Replace("#NAME#", name));

                AssetDatabase.ImportAsset(viewModelPath);

                AssetDatabase.ImportAsset(widgetPath);

                ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(widgetPath));
            }

            private static string Write_Internal(string folder, string file, string content)
            {
                var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, file).Replace('\\', '/'));

                File.WriteAllText(path, content.Replace("\r\n", "\n"));

                return path;
            }
        }
    }
}
