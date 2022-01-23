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
    internal sealed class GroupAssetTreeViewItem : TreeViewItem
    {
        public readonly GroupAsset data;

        public GroupAssetTreeViewItem(GroupAsset asset, int depth) : base(asset.path.GetHashCode(), depth, asset.path)
        {
            icon = AssetDatabase.GetCachedIcon(asset.path) as Texture2D;
            data = asset;
        }
    }

    internal class GroupAssetTreeView : TreeView
    {
        private readonly IDependenciesEditor _editor;
        public readonly List<GroupAsset> assets = new List<GroupAsset>();
        private readonly SortOption[] m_SortOptions = { SortOption.Asset, SortOption.Type, SortOption.Bundle, SortOption.Auto };
        private readonly List<TreeViewItem> result = new List<TreeViewItem>();

        public GroupAssetTreeView(TreeViewState treeViewState, MultiColumnHeaderState headerState,
            IDependenciesEditor editor) :
            base(treeViewState,
                new MultiColumnHeader(headerState))
        {
            _editor = editor;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.ResizeToFit();
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

        private IEnumerable<TreeViewItem> InitialOrder(IEnumerable<TreeViewItem> myTypes, int[] columnList)
        {
            var sortOption = m_SortOptions[columnList[0]];
            var ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
                case SortOption.Asset:
                    return myTypes.Order(l => l.displayName, ascending);
                case SortOption.Type:
                    return myTypes.Order(l => ((GroupAssetTreeViewItem)l).data.type, ascending);
                case SortOption.Bundle:
                    return myTypes.Order(l => ((GroupAssetTreeViewItem)l).data.bundle, ascending);
                case SortOption.Auto:
                    return myTypes.Order(l => ((GroupAssetTreeViewItem)l).data.auto, ascending);
            }

            return myTypes.Order(l => new FileInfo(l.displayName).Length, ascending);
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
                    headerContent = new GUIContent("Asset"),
                    minWidth = 320,
                    width = 480,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type"),
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
                    headerContent = new GUIContent("Auto"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                }
            };
            return retVal;
        }

        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);
            var items = Array.ConvertAll(GetSelection().ToArray(), o => (GroupAssetTreeViewItem)FindItem(o, rootItem));
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Path"), false,
                data =>
                {
                    EditorGUIUtility.systemCopyBuffer =
                        string.Join("\n", Array.ConvertAll(items, input => input.displayName));
                }, null);
            menu.AddItem(new GUIContent("Copy Bundle"), false,
                data =>
                {
                    EditorGUIUtility.systemCopyBuffer =
                        string.Join("\n", Array.ConvertAll(items, input => input.data.bundle));
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
            base.SingleClickedItem(id);
            var paths = new List<string>();
            var selection = GetSelection();
            foreach (var s in selection)
            {
                var item = FindItem(s, rootItem);
                paths.Add(item.displayName);
            }

            _editor.ReloadDependencies(paths.ToArray());
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var assetWithReferences = new Dictionary<string, List<GroupAsset>>();
            foreach (var asset in assets)
            {
                var dependencies = Settings.GetDependencies(asset.path);
                // 读取依赖，并为依赖生成被引用的对象。
                foreach (var dependency in dependencies)
                {
                    if (!assetWithReferences.TryGetValue(dependency, out var value))
                    {
                        value = new List<GroupAsset>();
                        assetWithReferences.Add(dependency, value);
                    }

                    value.Add(asset);
                }
            }

            foreach (var asset in assets)
            {
                var childItem = new GroupAssetTreeViewItem(asset, root.depth + 1);
                root.AddChild(childItem);
                if (!asset.auto)
                {
                    continue;
                }
                if (!assetWithReferences.TryGetValue(asset.path, out var references))
                {
                    continue;
                }
                foreach (var reference in references)
                {
                    childItem.AddChild(new GroupAssetTreeViewItem(reference, childItem.depth + 1));
                }
            }

            return root;
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
                        result.Add(current as GroupAssetTreeViewItem);
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
                var item = (GroupAssetTreeViewItem)args.item;
                if (item?.data == null)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        base.RowGUI(args);
                    }
                }
                else
                {
                    CellGUI(args.GetCellRect(i), (GroupAssetTreeViewItem)args.item, args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, GroupAssetTreeViewItem item, int column, ref RowGUIArgs args)
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
                    DefaultGUI.Label(cellRect, item.data.type, args.selected,
                        args.focused);
                    break;
                case 2:
                    item.data.bundle = Group.PackAsset(item.data);
                    DefaultGUI.Label(cellRect, item.data.bundle, args.selected, args.focused);
                    break;
                case 3:
                    DefaultGUI.Label(cellRect, item.data.auto.ToString(), args.selected,
                        args.focused);
                    break;
                case 4:
                    DefaultGUI.Label(cellRect, item.data.findReferences.ToString(), args.selected,
                        args.focused);
                    break;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetItem = FindItem(id, rootItem);
            if (assetItem == null)
            {
                return;
            }

            var o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.displayName);
            EditorGUIUtility.PingObject(o);
            Selection.activeObject = o;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null)
            {
                return;
            }

            var selectedObjects = new List<string>();
            foreach (var id in selectedIds)
            {
                var assetItem = FindItem(id, rootItem);
                if (assetItem == null || !assetItem.displayName.StartsWith("Assets/"))
                {
                    continue;
                }

                selectedObjects.Add(assetItem.displayName);
            }

            _editor.ReloadDependencies(selectedObjects.ToArray());
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

        private enum SortOption
        {
            Asset,
            Type,
            Bundle,
            Auto
        }
    }
}