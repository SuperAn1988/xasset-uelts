using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    public sealed class AssetTreeViewItem : TreeViewItem
    {
        public readonly string data;
        public string bundle;
        public long bundleSize;
        public long size;

        public AssetTreeViewItem(string asset, int depth) : base(asset.GetHashCode(), depth)
        {
            displayName = asset;
            icon = AssetDatabase.GetCachedIcon(displayName) as Texture2D;
            data = asset;
        }
    }

    public class AssetTreeView : TreeView
    {
        private readonly List<string> assets = new List<string>();

        private readonly SortOption[] m_SortOptions =
            { SortOption.Asset, SortOption.Size, SortOption.Bundle, SortOption.BundleSize };

        private readonly List<TreeViewItem> result = new List<TreeViewItem>();

        private Manifest _manifest;
        private ManifestsWindow _window;

        internal AssetTreeView(TreeViewState state, MultiColumnHeaderState headerState) : base(state,
            new MultiColumnHeader(headerState))
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.ResizeToFit();
        }

        internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }

        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    minWidth = 320,
                    width = 480,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Asset Size"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Bundle"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Bundle Size"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                }
            };
            return retVal;
        }


        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            foreach (var asset in assets)
            {
                var bundle = _manifest.GetBundle(asset);
                if (bundle == null)
                {
                    Debug.LogWarningFormat("no bundle for {0}", asset);
                    continue;
                }

                var item = new AssetTreeViewItem(asset, 0)
                {
                    size = GetAssetSize(asset),
                    bundle = bundle.nameWithAppendHash,
                    bundleSize = bundle.size
                };
                root.AddChild(item);
                var dependencies = Settings.GetDependencies(asset);
                foreach (var dependency in dependencies)
                {
                    bundle = _manifest.GetBundle(dependency);
                    if (bundle == null)
                    {
                        Debug.LogWarningFormat("no bundle for {0}", dependency);
                        continue;
                    }

                    item.AddChild(new AssetTreeViewItem(dependency, item.depth + 1)
                    {
                        size = GetAssetSize(dependency),
                        bundle = bundle.nameWithAppendHash,
                        bundleSize = bundle.size
                    });
                }
            }

            return root;
        }

        private static long GetAssetSize(string path)
        {
            var file = new FileInfo(path);
            return file.Exists ? file.Length : 0;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                SetSelection(Array.Empty<int>(), TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = (List<TreeViewItem>)base.BuildRows(root);
            if (!string.IsNullOrEmpty(searchString))
            {
                result.Clear();
                var stack = new Stack<TreeViewItem>();
                foreach (var element in root.children)
                {
                    stack.Push(element);
                }

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    // Matches search?
                    if (current.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(current as AssetTreeViewItem);
                    }
                    // if (current.children != null && current.children.Count > 0)
                    // {
                    //     foreach (var element in current.children)
                    //     {
                    //         stack.Push(element);
                    //     }
                    // }
                }

                rows = result;
            }

            SortIfNeeded(root, rows);
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var item = (AssetTreeViewItem)args.item;
                if (item?.data == null)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        base.RowGUI(args);
                    }
                }
                else
                {
                    CellGUI(args.GetCellRect(i), (AssetTreeViewItem)args.item, args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, AssetTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case 0:
                    cellRect.xMin += GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                    var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                    if (item.icon != null)
                    {
                        GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                    }

                    var content = item.displayName;
                    DefaultGUI.Label(
                        new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width,
                            cellRect.height),
                        content,
                        args.selected,
                        args.focused);
                    break;
                case 1:
                    DefaultGUI.Label(cellRect, UnityEditor.EditorUtility.FormatBytes(item.size), args.selected,
                        args.focused);
                    break;
                case 2:
                    DefaultGUI.Label(cellRect, item.bundle, args.selected, args.focused);
                    break;
                case 3:
                    DefaultGUI.Label(cellRect, UnityEditor.EditorUtility.FormatBytes(item.bundleSize), args.selected,
                        args.focused);
                    break;
            }
        }

        protected override void ContextClicked()
        {
            base.ContextClicked();
            var selection = GetSelection();
            var items = Array.ConvertAll(selection.ToArray(), o => FindItem(o, rootItem));
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false,
                data =>
                {
                    EditorGUIUtility.systemCopyBuffer =
                        string.Join("\n", Array.ConvertAll(items, input => input.displayName));
                }, null);
            var splitConfigs = EditorUtility.FindAssets<SplitConfig>();
            foreach (var config in splitConfigs)
            {
                EditorUtility.AddExportMenuItem(menu, config, Array.ConvertAll(items, input => input.displayName));
            }

            menu.ShowAsContext();
        }

        protected override void SingleClickedItem(int id)
        {
            base.ContextClickedItem(id);
            var selection = GetSelection();
            var items = Array.ConvertAll(selection.ToArray(), o => FindItem(o, rootItem));
            _window.ReloadDependencies(Array.ConvertAll(items, input => input.displayName));
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetItem = FindItem(id, rootItem);
            if (assetItem != null)
            {
                var o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.displayName);
                EditorGUIUtility.PingObject(o);
                Selection.activeObject = o;
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null)
            {
                return;
            }

            var selectedObjects = new List<Object>();
            foreach (var id in selectedIds)
            {
                var assetItem = FindItem(id, rootItem);
                if (assetItem == null || !assetItem.displayName.StartsWith("Assets/"))
                {
                    continue;
                }

                var o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.displayName);
                selectedObjects.Add(o);
                Selection.activeObject = o;
            }

            Selection.objects = selectedObjects.ToArray();
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            return true;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            if (DragAndDrop.paths.Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            return DragAndDropVisualMode.Rejected;
        }

        private void OnSortingChanged(MultiColumnHeader header)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        private void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return;
            }

            SortByColumn();

            rows.Clear();

            foreach (var t in root.children)
            {
                rows.Add(t);
                if (!t.hasChildren || t.children[0] == null || !IsExpanded(t.id))
                {
                    continue;
                }

                foreach (var child in t.children)
                {
                    rows.Add(child);
                }
            }

            Repaint();
        }

        private void SortByColumn()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            var assetList = new List<TreeViewItem>();
            foreach (var item in rootItem.children)
            {
                assetList.Add(item);
            }

            var orderedItems = InitialOrder(assetList, sortedColumns);
            rootItem.children = orderedItems.ToList();
        }

        public void SetAssets(ManifestsWindow window)
        {
            assets.Clear();
            _manifest = window.GetManifest();
            _window = window;
            foreach (var asset in window.GetSelectedAssets())
            {
                assets.Add(asset);
            }

            Reload();
        }

        private IEnumerable<TreeViewItem> InitialOrder(IEnumerable<TreeViewItem> myTypes, int[] columnList)
        {
            var sortOption = m_SortOptions[columnList[0]];
            var ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
                case SortOption.Asset:
                    return myTypes.Order(l => l.displayName, ascending);
                case SortOption.Bundle:
                    return myTypes.Order(l => ((AssetTreeViewItem)l).bundle, ascending);
                case SortOption.Size:
                    return myTypes.Order(l => ((AssetTreeViewItem)l).size, ascending);
                case SortOption.BundleSize:
                    return myTypes.Order(l => ((AssetTreeViewItem)l).bundleSize, ascending);
            }

            return myTypes.Order(l => new FileInfo(l.displayName).Length, ascending);
        }

        private enum SortOption
        {
            Asset,
            Bundle,
            Size,
            BundleSize
        }
    }
}