using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace xasset.editor
{
    public class ManifestsWindow : EditorWindow
    {
        private const int k_SearchHeight = 20;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderState;
        [SerializeField] private TreeViewState treeViewState;
        public bool reloadSelectedNow;

        [SerializeField] private TreeViewState dependenciesTreeViewState;
        private readonly GUIContent typeModule = new GUIContent("模块");

        private readonly List<string> types = new List<string>();
        private readonly Dictionary<string, List<string>> typeWithAssets = new Dictionary<string, List<string>>();

        private readonly GUIContent versionName = new GUIContent("模块");

        private Manifest _manifest;

        private BuildVersions _versions;
        private DependenciesTreeView dependenciesTreeView;

        private bool reloadAssets;
        private bool reloadNow = true;

        private SearchField searchField;

        private int selected;

        private AssetTreeView treeView;
        private VerticalSplitter verticalSplitter;
        private int selectedType { get; set; }

        private void OnEnable()
        {
            reloadNow = true;
        }

        private void OnGUI()
        {
            if (reloadNow)
            {
                Reload();
                reloadNow = false;
                reloadSelectedNow = true;
            }

            if (_versions.data.Count == 0)
            {
                EditorGUILayout.HelpBox("请先打包", MessageType.Info);
                return;
            }

            if (reloadSelectedNow)
            {
                var version = _versions.data[selected];
                typeWithAssets.Clear();
                types.Clear();
                var all = new List<string>();
                typeWithAssets.Add("All", all);
                types.Add("All");
                _manifest = Manifest.LoadFromFile(Settings.GetBuildPath(version.file));
                foreach (var asset in _manifest.GetAssets())
                {
                    if (!File.Exists(asset))
                    {
                        Debug.LogErrorFormat("文件不存在：{0}", asset);
                        continue;
                    }

                    var type = AssetDatabase.GetMainAssetTypeAtPath(asset);
                    if (!typeWithAssets.TryGetValue(type.Name, out var assets))
                    {
                        assets = new List<string>();
                        typeWithAssets.Add(type.Name, assets);
                        types.Add(type.Name);
                    }

                    assets.Add(asset);
                    all.Add(asset);
                }

                reloadSelectedNow = false;
                reloadAssets = true;
            }

            if (reloadAssets)
            {
                if (treeView != null)
                {
                    treeView.SetAssets(this);
                    reloadAssets = false;
                }
            }

            if (_versions == null || _versions.data.Count == 0)
            {
                GUILayout.Label("没有加载到当前平台的打包数据，请在打包后再打开此界面");
                return;
            }

            if (treeView == null)
            {
                if (treeViewState == null)
                {
                    treeViewState = new TreeViewState();
                }

                var headerState =
                    AssetTreeView.CreateDefaultMultiColumnHeaderState(); // multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
                }

                multiColumnHeaderState = headerState;
                treeView = new AssetTreeView(treeViewState, multiColumnHeaderState);
                treeView.Reload();
            }

            if (verticalSplitter == null)
            {
                verticalSplitter = new VerticalSplitter();
            }

            if (searchField == null)
            {
                searchField = new SearchField();
                searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
            }

            var rect = new Rect(0, 0, position.width, position.height);
            DrawTree(rect);
            DrawToolbar(new Rect(rect.xMin, rect.yMin, rect.width, k_SearchHeight));
        }

        private void Reload()
        {
            _versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
        }


        private void DrawManifest()
        {
            versionName.text = _versions.data[selected].name;
            var rect = GUILayoutUtility.GetRect(versionName, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, versionName, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            for (var index = 0; index < _versions.data.Count; index++)
            {
                var version = _versions.data[index];
                menu.AddItem(new GUIContent(version.name), selected == index,
                    data =>
                    {
                        selected = (int)data;
                        reloadSelectedNow = true;
                    }, index);
            }

            menu.DropDown(rect);
        }

        private void DrawTypes()
        {
            typeModule.text = types[selectedType];
            var rect = GUILayoutUtility.GetRect(typeModule, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, typeModule, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            for (var index = 0; index < types.Count; index++)
            {
                var type = types[index];
                menu.AddItem(new GUIContent(type), selectedType == index,
                    data =>
                    {
                        selectedType = (int)data;
                        reloadAssets = true;
                    }, index);
            }

            menu.DropDown(rect);
        }

        public List<string> GetSelectedAssets()
        {
            return selectedType < types.Count ? typeWithAssets[types[selectedType]] : new List<string>();
        }

        private void DrawToolbar(Rect toolbarPos)
        {
            GUILayout.BeginArea(new Rect(0, 0, toolbarPos.width, k_SearchHeight * 2));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                DrawManifest();
                DrawTypes();
                treeView.searchString = searchField.OnToolbarGUI(treeView.searchString);
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(128)))
                {
                    SaveSelected();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void SaveSelected()
        {
            var path = UnityEditor.EditorUtility.SaveFilePanel("Save File", "",
                $"DependenciesWith{_manifest.name}For{types[selectedType]}s",
                "txt");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ShowNotification(new GUIContent("Save Success!"));
            var assets = GetSelectedAssets();
            assets.Sort((a, b) => GetBundlesSize(b, out _).CompareTo(GetBundlesSize(a, out _)));
            var sb = new StringBuilder();
            foreach (var asset in assets)
            {
                var size = GetBundlesSize(asset, out var bundles);
                sb.AppendLine($"{asset}({UnityEditor.EditorUtility.FormatBytes(size)})");
                sb.AppendLine(" - Bundles:" + UnityEditor.EditorUtility.FormatBytes(size));
                bundles.Sort((a, b) => b.size.CompareTo(a.size));
                foreach (var bundle in bundles)
                {
                    sb.AppendLine(
                        $"  - {bundle.nameWithAppendHash}({UnityEditor.EditorUtility.FormatBytes(bundle.size)})");
                }

                sb.AppendLine(" - Dependencies:");
                var dependencies = new List<string>();
                foreach (var dependency in AssetDatabase.GetDependencies(asset))
                {
                    if (asset == dependency || !_manifest.Contains(dependency))
                    {
                        continue;
                    }

                    dependencies.Add(dependency);
                }

                dependencies.Sort((a, b) => GetBundlesSize(b, out _).CompareTo(GetBundlesSize(a, out _)));
                foreach (var dependency in dependencies)
                {
                    sb.AppendLine(
                        $"  - {dependency}({UnityEditor.EditorUtility.FormatBytes(GetBundlesSize(dependency, out _))})");
                }
            }

            File.WriteAllText(path, sb.ToString());
            UnityEditor.EditorUtility.OpenWithDefaultApp(path);
        }

        public void ReloadDependencies(params string[] assetPaths)
        {
            if (dependenciesTreeView == null)
            {
                return;
            }

            dependenciesTreeView.assetPaths.Clear();
            var size = 0L;
            var bundles = new List<ManifestBundle>();
            foreach (var assetPath in assetPaths)
            {
                var bundle = _manifest.GetBundle(assetPath);
                if (bundle == null || bundles.Contains(bundle))
                {
                    continue;
                }

                bundles.Add(bundle);
                var dependencies = _manifest.GetDependencies(bundle);
                foreach (var dependency in dependencies)
                {
                    if (bundles.Contains(dependency))
                    {
                        continue;
                    }

                    bundles.Add(dependency);
                    size += dependency.size;
                }
            }

            bundles.Sort((a, b) => b.size.CompareTo(a.size));
            foreach (var bundle in bundles)
            {
                dependenciesTreeView.assetPaths.Add(
                    $"({UnityEditor.EditorUtility.FormatBytes(bundle.size)})\t{bundle.nameWithAppendHash}");
            }

            dependenciesTreeView.title = $"Bundles ({UnityEditor.EditorUtility.FormatBytes(size)})";
            dependenciesTreeView.Reload();
            dependenciesTreeView.ExpandAll();
        }

        public long GetBundlesSize(string asset, out List<ManifestBundle> bundles)
        {
            var manifest = _manifest;
            bundles = new List<ManifestBundle>();
            var bundlesSize = 0L;
            var bundle = manifest.GetBundle(asset);
            bundlesSize += bundle.size;
            bundles.Add(bundle);
            var dependencies = manifest.GetDependencies(bundle);
            if (dependencies == null)
            {
                return bundlesSize;
            }

            foreach (var dependency in dependencies)
            {
                bundlesSize += dependency.size;
                bundles.Add(dependency);
            }

            return bundlesSize;
        }

        private void DrawTree(Rect rect)
        {
            const int toolbarHeight = k_SearchHeight + 4;
            var treeRect = new Rect(
                rect.xMin,
                rect.yMin + toolbarHeight,
                rect.width,
                verticalSplitter.rect.y - toolbarHeight);

            treeView.OnGUI(treeRect);
            verticalSplitter.OnGUI(rect);
            if (verticalSplitter.resizing)
            {
                Repaint();
            }

            if (dependenciesTreeViewState == null)
            {
                dependenciesTreeViewState = new TreeViewState();
            }

            if (dependenciesTreeView == null)
            {
                dependenciesTreeView = new DependenciesTreeView(dependenciesTreeViewState)
                    { topOnly = true, title = "Bundles" };
                dependenciesTreeView.Reload();
            }

            dependenciesTreeView.OnGUI(new Rect(treeRect.x, verticalSplitter.rect.y + 4, treeRect.width,
                rect.height - treeRect.yMax - 4));
        }

        public Manifest GetManifest()
        {
            return _manifest;
        }
    }
}