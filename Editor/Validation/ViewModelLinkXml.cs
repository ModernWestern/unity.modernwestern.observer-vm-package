using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// View Bindings read ViewModel members by name, so code stripping (IL2CPP, Medium/High) could remove members no script uses.
    /// On every build this keeps the ViewModel type of every Widget in the project.
    /// </summary>
    internal sealed class ViewModelLinkXml : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var path = Path.GetFullPath(Path.Combine("Library", "ObserverVM", "ViewModels.link.xml"));

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Library");

            File.WriteAllText(path, Build());

            return path;
        }

        /// <summary>
        /// The link.xml content: every ViewModel type used by a Widget, preserved whole.
        /// </summary>
        public static string Build()
        {
            var viewModels = TypeCache.GetTypesDerivedFrom<Widget>()
                                      .Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                                      .Select(ViewModelTypeOf)
                                      .Where(type => type != null && !type.ContainsGenericParameters)
                                      .Distinct()
                                      .ToArray();

            var xml = new StringBuilder("<linker>\n");

            foreach (var assembly in viewModels.GroupBy(type => type.Assembly.GetName().Name).OrderBy(group => group.Key))
            {
                // ignoreIfMissing: test or editor-only assemblies are not part of every build.
                xml.Append("  <assembly fullname=\"").Append(assembly.Key).Append("\" ignoreIfMissing=\"1\">\n");

                foreach (var type in assembly.OrderBy(type => type.FullName))
                {
                    xml.Append("    <type fullname=\"").Append(type.FullName?.Replace('+', '/')).Append("\" preserve=\"all\" />\n");
                }

                xml.Append("  </assembly>\n");
            }

            return xml.Append("</linker>\n").ToString();
        }

        public static Type ViewModelTypeOf(Type widgetType)
        {
            for (var type = widgetType; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Widget<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

#pragma warning disable CS0618
        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }
#pragma warning restore CS0618
    }
}
