using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace xasset.editor
{
    public sealed class BuildRecordTreeViewItem : TreeViewItem
    {
        public readonly BuildRecord data;

        public BuildRecordTreeViewItem(BuildRecord buildRecord, int depth) : base(buildRecord.file.GetHashCode(),
            depth)
        {
            data = buildRecord;
            displayName = data.file;
        }
    }

    public class BuildRecordTreeView : TreeView
    {
        private readonly SortOption[] m_SortOptions =
            { SortOption.Name, SortOption.Size, SortOption.Time, SortOption.Elapsed, SortOption.Platform };

        private readonly List<TreeViewItem> result = new List<TreeViewItem>();
        private VersionsWindow _window;

        internal BuildRecordTreeView(TreeViewState state, MultiColumnHeaderState headerState) : base(state,
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
                    headerContent = new GUIContent("Name"),
                    minWidth = 240,
                    width = 240,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Time"),
                    minWidth = 128,
                    width = 160,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Elapsed"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Platform"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                }
            };
            return retVal;
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

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count <= 0) return;
            var id = selectedIds[0];
            var item = (BuildRecordTreeViewItem)FindItem(id, rootItem);
            if (item != null)
            {
                _window.SetRecord(item.data);
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
                        result.Add(current as BuildRecordTreeViewItem);
                    }

                    if (current.children != null && current.children.Count > 0)
                    {
                        foreach (var element in current.children)
                        {
                            stack.Push(element);
                        }
                    }
                }

                rows = result;
            }

            SortIfNeeded(root, rows);
            return rows;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            var records = BuildRecords.GetRecords();
            foreach (var asset in records.data)
            {
                var item = new BuildRecordTreeViewItem(asset, 0);
                root.AddChild(item);
            }

            return root;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return false;
        }

        protected override void SingleClickedItem(int id)
        {
            base.ContextClickedItem(id);
            var item = (BuildRecordTreeViewItem)FindItem(id, rootItem);
            if (item != null)
            {
                _window.SetRecord(item.data);
            }
        }

        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);
            var item = (BuildRecordTreeViewItem)FindItem(id, rootItem);
            var selection = GetSelection();
            var items = Array.ConvertAll(selection.ToArray(), o => (BuildRecordTreeViewItem)FindItem(o, rootItem));
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Open"), false, data =>
                {
                    foreach (var recordItem in items)
                    {
                        var path = $"{Utility.buildPath}/{recordItem.data.platform}/{recordItem.data.file}";
                        UnityEditor.EditorUtility.OpenWithDefaultApp(path);
                    }
                },
                null);

            menu.AddItem(new GUIContent("Copy to StreamingAssets"), false,
                data => { BuildScript.CopyToStreamingAssets(Array.ConvertAll(items, input => input.data)); }, null);

            menu.AddItem(new GUIContent("Rebase"), false, data =>
                {
                    foreach (var recordItem in items)
                    {
                        _window.Rebase(recordItem.data);
                    }
                },
                null);

            menu.AddItem(new GUIContent("Remove"), false, data =>
            {
                var records = BuildRecords.GetRecords();
                foreach (var viewItem in items)
                {
                    records.data.Remove(viewItem.data);
                }

                EditorUtility.SaveAsset(records);
                Reload();
            }, null);
            EditorUtility.customContextMenu?.Invoke(menu, item.data);
            menu.ShowAsContext();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var item = (BuildRecordTreeViewItem)args.item;
                if (item?.data == null)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        base.RowGUI(args);
                    }
                }
                else
                {
                    CellGUI(args.GetCellRect(i), (BuildRecordTreeViewItem)args.item, args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, BuildRecordTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case 0:
                    cellRect.xMin += GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                    DefaultGUI.Label(cellRect, item.displayName, args.selected, args.focused);
                    break;
                case 1:
                    DefaultGUI.Label(cellRect, UnityEditor.EditorUtility.FormatBytes(item.data.size), args.selected,
                        args.focused);
                    break;
                case 2:
                    DefaultGUI.Label(cellRect, item.data.date, args.selected, args.focused);
                    break;

                case 3:
                    DefaultGUI.Label(cellRect, $"{item.data.elapsed}s", args.selected, args.focused);
                    break;
                case 4:
                    DefaultGUI.Label(cellRect, item.data.platform, args.selected, args.focused);
                    break;
            }
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

        private IEnumerable<TreeViewItem> InitialOrder(IEnumerable<TreeViewItem> myTypes, int[] columnList)
        {
            var sortOption = m_SortOptions[columnList[0]];
            var ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
                case SortOption.Name:
                    return myTypes.Order(l => l.displayName, ascending);
                case SortOption.Size:
                    return myTypes.Order(l => ((BuildRecordTreeViewItem)l).data.size, ascending);
                case SortOption.Time:
                    return myTypes.Order(l => ((BuildRecordTreeViewItem)l).data.timestamp, ascending);
                case SortOption.Elapsed:
                    return myTypes.Order(l => ((BuildRecordTreeViewItem)l).data.elapsed, ascending);
                case SortOption.Platform:
                    return myTypes.Order(l => ((BuildRecordTreeViewItem)l).data.platform, ascending);
            }

            return myTypes.Order(l => new FileInfo(l.displayName).Length, ascending);
        }

        public void SetWindow(VersionsWindow versionsWindow)
        {
            _window = versionsWindow;
        }

        private enum SortOption
        {
            Name,
            Size,
            Time,
            Elapsed,
            Platform
        }
    }
}