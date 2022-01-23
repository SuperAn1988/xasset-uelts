using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace xasset.editor
{
    public class BuildsWindow : EditorWindow, IDependenciesEditor
    {
        private const int k_SearchHeight = 20;
        [SerializeField] private TreeViewState _treeViewState;
        [SerializeField] private MultiColumnHeaderState _multiColumnHeaderState;
        [SerializeField] private TreeViewState _dependenciesTreeViewState;

        private readonly List<Build> _builds = new List<Build>();
        private readonly GUIContent buildName = new GUIContent("Build");

        private Build _build;

        private DependenciesTreeView _dependenciesTreeView;

        private SearchField _searchField;
        private GroupAssetTreeView _treeView;

        private bool initialized;

        private bool rebuildNow;

        private int selected;
        private VerticalSplitter verticalSplitter;

        private void Update()
        {
            if (rebuildNow)
            {
                rebuildNow = false;
                BuildScript.BuildBundles(new BuildTask(_build));
            }
        }

        public void OnGUI()
        {
            if (verticalSplitter == null)
            {
                verticalSplitter = new VerticalSplitter();
            }

            if (_dependenciesTreeViewState == null)
            {
                _dependenciesTreeViewState = new TreeViewState();
            }

            if (_dependenciesTreeView == null)
            {
                _dependenciesTreeView = new DependenciesTreeView(_dependenciesTreeViewState);
                _dependenciesTreeView.Reload();
            }

            if (_treeViewState == null)
            {
                _treeViewState = new TreeViewState();
                var headerState =
                    GroupAssetTreeView.CreateDefaultMultiColumnHeaderState(); // multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(_multiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(_multiColumnHeaderState, headerState);
                }

                _multiColumnHeaderState = headerState;
            }

            if (_treeView == null)
            {
                _treeView = new GroupAssetTreeView(_treeViewState, _multiColumnHeaderState, this);
                _treeView.Reload();
            }

            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            }

            Initialize();

            if (_builds.Count == 0)
            {
                GUILayout.Label("No builds");
                return;
            }

            var rect = new Rect(0, 0, position.width, position.height);

            DrawToolbar();
            DrawTreeView(rect);
        }

        public void ReloadDependencies(params string[] assetPaths)
        {
            if (_dependenciesTreeView == null)
            {
                return;
            }

            _dependenciesTreeView.assetPaths.Clear();
            _dependenciesTreeView.assetPaths.AddRange(assetPaths);
            _dependenciesTreeView.Reload();
            _dependenciesTreeView.ExpandAll();
        }

        private void DrawTreeView(Rect rect)
        {
            verticalSplitter.OnGUI(rect);
            if (verticalSplitter.resizing)
            {
                Repaint();
            }

            const int toolbarHeight = k_SearchHeight + 4;
            var treeRect = new Rect(
                rect.xMin,
                rect.yMin + toolbarHeight,
                rect.width,
                verticalSplitter.rect.y - toolbarHeight);

            _treeView?.OnGUI(treeRect);

            var rect2 = new Rect(treeRect.x, verticalSplitter.rect.y + 4, treeRect.width,
                rect.height - treeRect.yMax - 4);
            _dependenciesTreeView?.OnGUI(rect2);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawBuild();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    Reload();
                }

                if (GUILayout.Button("Find References", EditorStyles.toolbarButton))
                {
                    BuildScript.FindReferences();
                }

                if (_treeView != null)
                {
                    _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Build Bundles", EditorStyles.toolbarButton))
                {
                    rebuildNow = true;
                }

                if (GUILayout.Button("Inspector", EditorStyles.toolbarButton))
                {
                    EditorUtility.PingWithSelected(_build);
                }
            }
        }

        private void DrawBuild()
        {
            buildName.text = _build.name;
            var rect = GUILayoutUtility.GetRect(buildName, EditorStyles.toolbarDropDown, GUILayout.MinWidth(128));
            if (!EditorGUI.DropdownButton(rect, buildName, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            for (var index = 0; index < _builds.Count; index++)
            {
                var version = _builds[index];
                menu.AddItem(new GUIContent(version.name), selected == index,
                    data =>
                    {
                        if (selected != (int)data)
                        {
                            selected = (int)data;
                            _build = _builds[selected];
                            Reload();
                        }
                    }, index);
            }

            menu.DropDown(rect);
        }

        private void Reload()
        {
            if (_treeView == null)
            {
                return;
            }
            _treeView.assets.Clear();
            var task = new BuildTask(_build);
            var collectAssets = new CollectAssets(task, _build.groups);
            collectAssets.Run();
            _treeView.assets.AddRange(task.rawAssets);
            _treeView.assets.AddRange(task.bundledAssets);
            _treeView.Reload();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            _builds.AddRange(EditorUtility.FindAssets<Build>());
            if (_builds.Count > 0)
            {
                _build = _builds[selected];
                Reload();
            }

            initialized = true;
        }
    }
}