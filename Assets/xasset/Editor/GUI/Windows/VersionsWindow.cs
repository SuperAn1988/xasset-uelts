using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace xasset.editor
{
    public class VersionsWindow : EditorWindow
    {
        private const int k_SearchHeight = 20;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderState;
        [SerializeField] private TreeViewState treeViewState;
        [SerializeField] private TreeViewState filesTreeViewState;

        private readonly GUIContent buildBundles = new GUIContent("Build Bundles");
        private readonly GUIContent buildClear = new GUIContent("Clear");
        private readonly GUIContent buildOpen = new GUIContent("Open");
        private readonly GUIContent buildPlayer = new GUIContent("Build Player");
        private readonly GUIContent buildTools = new GUIContent("Tools");

        private readonly List<Action> runOnUpdates = new List<Action>();
        private SearchField _searchField;


        private FilesTreeView filesTreeView;

        private BuildRecordTreeView treeView;
        private VerticalSplitter verticalSplitter;

        private void Update()
        {
            foreach (var runOnUpdate in runOnUpdates)
            {
                runOnUpdate.Invoke();
            }
            runOnUpdates.Clear();
        }

        private void OnEnable()
        {
            Settings.GetDefaultSettings().Initialize();
        }

        private void OnGUI()
        {
            if (verticalSplitter == null)
            {
                verticalSplitter = new VerticalSplitter();
            }

            if (filesTreeViewState == null)
            {
                filesTreeViewState = new TreeViewState();
            }

            if (filesTreeView == null)
            {
                filesTreeView = new FilesTreeView(filesTreeViewState);
            }

            if (treeView == null)
            {
                if (treeViewState == null)
                {
                    treeViewState = new TreeViewState();
                }

                var headerState =
                    BuildRecordTreeView.CreateDefaultMultiColumnHeaderState(); // multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
                }

                multiColumnHeaderState = headerState;
                treeView = new BuildRecordTreeView(treeViewState, multiColumnHeaderState);
                treeView.SetWindow(this);
                treeView.Reload();
            }

            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
            }

            var rect = new Rect(0, 0, position.width, position.height);
            DrawTree(rect);
            DrawToolbar(new Rect(rect.xMin, rect.yMin, rect.width, k_SearchHeight));
        }

        private void DrawBuildPlayer()
        {
            var rect = GUILayoutUtility.GetRect(buildPlayer, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, buildPlayer, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            var configs = EditorUtility.FindAssets<SplitConfig>();
            for (var index = 0; index < configs.Length; index++)
            {
                var config = configs[index];
                menu.AddItem(new GUIContent("Build Player for " + config.name), false,
                    data =>
                    {
                        var settings = Settings.GetDefaultSettings();
                        settings.splitConfig = config;
                        settings.Save();
                        BuildScript.BuildPlayer();
                        Refresh();
                    }, index);
            }

            menu.AddItem(new GUIContent("Build Player Without SplitConfig"), false,
                data =>
                {
                    BuildScript.BuildPlayer();
                    Refresh();
                }, null);
            menu.DropDown(rect);
        }

        private void DrawBuildBundles()
        {
            var rect = GUILayoutUtility.GetRect(buildBundles, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, buildBundles, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            var builds = Build.GetAllBuilds();
            foreach (var build in builds)
            {
                menu.AddItem(new GUIContent("Build Bundles for " + build.name), false,
                    data =>
                    {
                        runOnUpdates.Add(() =>
                        {
                            BuildScript.BuildBundles(new BuildTask((Build)data));
                            Refresh();
                        });
                    }, build);
            }

            menu.AddItem(new GUIContent("Build Bundles for All"), false,
                data =>
                {
                    runOnUpdates.Add(() =>
                    {
                        BuildScript.BuildBundles();
                        Refresh();
                    });
                }, null);
            menu.DropDown(rect);
        }

        private void DrawToolbar(Rect toolbarPos)
        {
            using (new GUILayout.AreaScope(new Rect(0, 0, toolbarPos.width, k_SearchHeight * 2)))
            {
                using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
                    {
                        EditorUtility.PingWithSelected(Settings.GetDefaultSettings());
                    }

                    GUILayout.FlexibleSpace();
                    DrawTools();
                    DrawBuildBundles();
                    DrawBuildPlayer();
                    DrawClear();
                    DrawOpen();
                    treeView.searchString = _searchField.OnToolbarGUI(treeView.searchString);
                }
            }
        }

        private void DrawTools()
        {
            var rect = GUILayoutUtility.GetRect(buildTools, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, buildTools, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Builds"), false,
                data => { MenuItems.OpenBuilds(); }, null);
            menu.AddItem(new GUIContent("Manifests"), false,
                data => { MenuItems.OpenManifests(); }, null);
            menu.AddItem(new GUIContent("Loadables"), false,
                data => { MenuItems.OpenLoadables(); }, null);

            menu.DropDown(rect);
        }

        private void DrawOpen()
        {
            var rect = GUILayoutUtility.GetRect(buildOpen, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, buildOpen, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Open Build"), false,
                data => { UnityEditor.EditorUtility.OpenWithDefaultApp(Settings.PlatformBuildPath); }, null);
            menu.AddItem(new GUIContent("Open Download"), false,
                data => { UnityEditor.EditorUtility.OpenWithDefaultApp(Application.persistentDataPath); }, null);
            menu.AddItem(new GUIContent("Open Temporary"), false,
                data => { UnityEditor.EditorUtility.OpenWithDefaultApp(Application.temporaryCachePath); }, null);
            menu.DropDown(rect);
        }

        private void DrawClear()
        {
            var rect = GUILayoutUtility.GetRect(buildClear, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, buildClear, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Clear History"), false,
                data =>
                {
                    MenuItems.ClearHistory();
                    Refresh();
                }, null);
            menu.AddItem(new GUIContent("Clear Build"), false,
                data =>
                {
                    BuildScript.ClearBuild();
                    Refresh();
                }, null);
            menu.DropDown(rect);
        }

        public void Rebase(BuildRecord buildRecord)
        {
            if (buildRecord == null)
            {
                return;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var manifest = CreateInstance<Manifest>();
            manifest.Load(Settings.GetBuildPath(buildRecord.file));
            var task = new BuildTask(buildRecord.build);
            task.jobs.Add(new RebaseManifest(task, manifest)); // 只生成清单不生成二进制。
            task.Run();
            Refresh();
        }

        private void Refresh()
        {
            if (treeView == null)
            {
                return;
            }

            treeView.Reload();
            treeView.Repaint();
        }

        private void SetFiles(IEnumerable<string> files)
        {
            filesTreeView?.SetFiles(files);
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

            filesTreeView.OnGUI(new Rect(treeRect.x, verticalSplitter.rect.y + 4, treeRect.width,
                rect.height - treeRect.yMax - 4));
        }

        public void SetRecord(BuildRecord itemData)
        {
            SetFiles(itemData.changes);
        }
    }
}