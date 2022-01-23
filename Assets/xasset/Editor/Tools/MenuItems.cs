using System;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    public static class MenuItems
    {
        [MenuItem("xasset/Build Bundles", false, 10)]
        public static void BuildBundles()
        {
            BuildScript.BuildBundles();
        }

        [MenuItem("xasset/Build Bundles with Selection(Builds) ", false, 10)]
        public static void BuildBundlesWithSelection()
        {
            BuildScript.BuildBundlesWithSelection();
        }

        [MenuItem("xasset/Build Player", false, 10)]
        public static void BuildPlayer()
        {
            BuildScript.BuildPlayer();
        }

        [MenuItem("xasset/Copy Build to StreamingAssets ", false, 50)]
        public static void CopyBuildToStreamingAssets()
        {
            BuildScript.CopyToStreamingAssets();
        }

        [MenuItem("xasset/Find References", false, 50)]
        public static void FindReferences()
        {
            BuildScript.FindReferences();
        }

        [MenuItem("xasset/Versions", false, 100)]
        private static void OpenVersions()
        {
            EditorWindow.GetWindow<VersionsWindow>(false, "Versions");
        }

        [MenuItem("xasset/Builds", false, 100)]
        public static void OpenBuilds()
        {
            EditorWindow.GetWindow<BuildsWindow>(false, "Builds");
        }

        [MenuItem("xasset/Manifests", false, 100)]
        public static void OpenManifests()
        {
            EditorWindow.GetWindow<ManifestsWindow>(false, "Manifests");
        }

        [MenuItem("xasset/Loadables", false, 100)]
        public static void OpenLoadables()
        {
            EditorWindow.GetWindow<LoadablesWindow>(false, "Loadables");
        }

        [MenuItem("xasset/Clear Build", false, 800)]
        public static void ClearBuild()
        {
            BuildScript.ClearBuild();
        }

        [MenuItem("xasset/Clear Build from selection", false, 800)]
        public static void ClearBuildFromSelection()
        {
            BuildScript.ClearBuildFromSelection();
        }

        [MenuItem("xasset/Clear History", false, 800)]
        public static void ClearHistory()
        {
            BuildScript.ClearHistory();
        }

        [MenuItem("xasset/Create Command Tools", false, 1000)]
        public static void CreateCommandTools()
        {
            CommandLine.CreateTools(typeof(CommandLine).FullName, nameof(CommandLine.BuildBundles),
                "-build %1 -version %2");
            CommandLine.CreateTools(typeof(CommandLine).FullName, nameof(CommandLine.BuildPlayer),
                "-config %1 -offline %2");
            UnityEditor.EditorUtility.OpenWithDefaultApp(Environment.CurrentDirectory);
        }

        [MenuItem("xasset/Documentation", false, 2000)]
        private static void OpenDocumentation()
        {
            GotoHomepage();
        }

        public static void GotoHomepage(string location = null)
        {
            Application.OpenURL(
                string.IsNullOrEmpty(location) ? "https://xasset.pro" : $"https://xasset.pro/{location}");
        }

        [MenuItem("xasset/File a Bug", false, 2000)]
        public static void FileABug()
        {
            Application.OpenURL("https://github.com/xasset/xasset/issues");
        }


        [MenuItem("Assets/Compute Hash", false, 2000)]
        public static void ComputeHash()
        {
            var target = Selection.activeObject;
            var path = AssetDatabase.GetAssetPath(target);
            var hash = Utility.ComputeHash(path);
            Debug.LogFormat("Compute Hash for {0} with {1}", path, hash);
        }
    }
}