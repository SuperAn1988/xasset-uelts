using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    public class LoadablesWindow : EditorWindow
    {
        [SerializeField] private MultiColumnHeaderState m_MultiColumnHeaderState;
        [SerializeField] private TreeViewState m_TreeViewState;

        private readonly GUIContent exportLoadables =
            new GUIContent("Export to...", "Export recorded loadables to split config");

        private readonly Dictionary<int, List<Loadable>> frameWithLoadables = new Dictionary<int, List<Loadable>>();

        private readonly List<Loadable> loads = new List<Loadable>();

        private readonly GUIContent scriptPlayMode = new GUIContent("ScriptPlayMode");
        private readonly List<Loadable> unloads = new List<Loadable>();

        private Mode _mode = Mode.Current;

        private SearchField _searchField;

        private Settings _settings;

        private int current;

        private int frame;

        private bool initialized;

        private List<Loadable> loadables = new List<Loadable>();

        private LoadableTreeView m_TreeView;

        private bool recording = true;

        private int selected;

        private void Update()
        {
            if (recording && Application.isPlaying)
            {
                TakeASample();
            }
        }

        private void OnEnable()
        {
            Loadable.onLoad = OnLoad;
            Loadable.onUnloaded = OnUnloaded;
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                _settings = Settings.GetDefaultSettings();
            }

            if (m_TreeView == null)
            {
                m_TreeViewState = new TreeViewState();
                var headerState =
                    LoadableTreeView.CreateDefaultMultiColumnHeaderState(); // multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                }

                m_MultiColumnHeaderState = headerState;
                m_TreeView = new LoadableTreeView(m_TreeViewState, headerState);
                m_TreeView.SetAssets(loadables);
            }

            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                recording = GUILayout.Toggle(recording, "Record", EditorStyles.toolbarButton);
                EditorGUI.BeginChangeCheck();
                _mode = (Mode)EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarDropDown, GUILayout.Width(64));
                if (EditorGUI.EndChangeCheck())
                {
                    TakeASample();
                }

                m_TreeView.searchString = _searchField.OnToolbarGUI(m_TreeView.searchString);

                GUILayout.FlexibleSpace();
                DrawScriptPlayMode();
                DrawExport();
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                {
                    frame = 0;
                    loadables.Clear();
                    ReloadFrameData();
                }

                GUILayout.Label("Frame:", EditorStyles.miniLabel, GUILayout.Width(48));
                if (GUILayout.Button("<", EditorStyles.toolbarButton))
                {
                    frame = Mathf.Max(0, frame - 1);
                    ReloadFrameData();
                    recording = false;
                }

                if (GUILayout.Button("Current", EditorStyles.toolbarButton))
                {
                    TakeASample();
                    recording = false;
                }

                if (GUILayout.Button(">", EditorStyles.toolbarButton))
                {
                    frame = Mathf.Min(frame + 1, Time.frameCount);
                    ReloadFrameData();
                    recording = false;
                }

                EditorGUI.BeginChangeCheck();
                frame = EditorGUILayout.IntSlider(frame, 0, current);
                if (EditorGUI.EndChangeCheck())
                {
                    recording = false;
                    ReloadFrameData();
                }
            }

            var treeRect = GUILayoutUtility.GetLastRect();
            m_TreeView.OnGUI(new Rect(0, treeRect.yMax, position.width, position.height - treeRect.yMax));
        }


        private void OnUnloaded(Loadable loadable)
        {
            if (loadable is Dependencies)
            {
                return;
            }
            if (unloads.Exists(o => o.pathOrURL == loadable.pathOrURL))
            {
                return;
            }
            unloads.Add(loadable);
        }


        private void OnLoad(Loadable loadable)
        {
            if (loadable is Dependencies)
            {
                return;
            }
            if (loads.Exists(o => o.pathOrURL == loadable.pathOrURL))
            {
                return;
            }
            loads.Add(loadable);
        }

        private void DrawScriptPlayMode()
        {
            scriptPlayMode.text = "Script Play Mode->" + _settings.scriptPlayMode;
            var rect = GUILayoutUtility.GetRect(scriptPlayMode, EditorStyles.toolbarDropDown, GUILayout.Width(172));
            if (!EditorGUI.DropdownButton(rect, scriptPlayMode, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            var modes = Enum.GetValues(typeof(ScriptPlayMode));
            foreach (var mode in modes)
            {
                menu.AddItem(new GUIContent(mode.ToString()), (ScriptPlayMode)mode == _settings.scriptPlayMode, data =>
                {
                    _settings.scriptPlayMode = (ScriptPlayMode)data;
                    EditorUtility.SaveAsset(_settings);
                }, mode);
            }

            menu.DropDown(rect);
        }

        private void DrawExport()
        {
            var rect = GUILayoutUtility.GetRect(exportLoadables, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, exportLoadables, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            var configs = EditorUtility.FindAssets<SplitConfig>();
            if (configs.Length == 0)
            {
                ShowNotification(new GUIContent("找不到分包配置，请先创建！"));
            }

            foreach (var config in configs)
            {
                menu.AddItem(new GUIContent(config.name + "(SplitConfig)"), false,
                    data =>
                    {
                        if (Settings.GetDefaultSettings().scriptPlayMode != ScriptPlayMode.Simulation)
                        {
                            ShowNotification(new GUIContent("请在仿真模式使用导出分包配置功能！"));
                            return;
                        }

                        var assets = new HashSet<Object>();
                        foreach (var loadable in loadables)
                        {
                            switch (loadable)
                            {
                                case Asset asset:
                                    assets.Add(asset.asset);
                                    break;
                                case Scene scene:
                                    assets.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.pathOrURL));
                                    break;
                            }
                        }

                        assets.UnionWith(config.assets);
                        config.assets = assets.ToArray();
                        EditorUtility.SaveAsset(config);
                        EditorUtility.PingWithSelected(config);
                        ShowNotification(new GUIContent("导出成功！"));
                    }, config);
            }

            menu.AddItem(new GUIContent("csv"), false, data =>
            {
                var path = UnityEditor.EditorUtility.SaveFilePanel("Save", "",
                    "loadables_" + BuildScript.GetTimeForNow(), "csv");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("Path, Elapsed, Frames, Load Scene, Unload Scene, Reference Count");
                foreach (var loadable in loadables)
                {
                    sb.AppendLine(
                        $"{loadable.pathOrURL},{loadable.elapsed},{loadable.frames},{loadable.loadScene},{loadable.unloadScene},{loadable.referenceCount}");
                }

                File.WriteAllText(path, sb.ToString());
                UnityEditor.EditorUtility.OpenWithDefaultApp(path);

                ShowNotification(new GUIContent(path + "保存成功！"));
            }, null);
            menu.DropDown(rect);
        }


        private void ReloadFrameData()
        {
            if (m_TreeView != null)
            {
                m_TreeView.SetAssets(
                    frameWithLoadables.TryGetValue(frame, out var value) ? value : new List<Loadable>());
            }
        }

        private void TakeASample()
        {
            current = frame = Time.frameCount;
            loadables = new List<Loadable>();

            switch (_mode)
            {
                case Mode.Current:
                    foreach (var item in Asset.Cache.Values)
                    {
                        if (item.isDone)
                        {
                            loadables.Add(item);
                        }
                    }

                    foreach (var item in Bundle.Cache.Values)
                    {
                        if (item.isDone)
                        {
                            loadables.Add(item);
                        }
                    }

                    foreach (var item in RawAsset.Cache.Values)
                    {
                        if (item.isDone)
                        {
                            loadables.Add(item);
                        }
                    }

                    if (Scene.main != null && Scene.main.isDone)
                    {
                        loadables.Add(Scene.main);
                        loadables.AddRange(Scene.main.additives);
                    }

                    break;
                case Mode.Loads:
                    loadables.AddRange(loads);
                    break;
                case Mode.Unloads:
                    loadables.AddRange(unloads);
                    break;
            }

            frameWithLoadables[frame] = loadables;
            ReloadFrameData();
        }

        private enum Mode
        {
            Current,
            Loads,
            Unloads
        }
    }
}