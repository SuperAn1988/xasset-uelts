using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace xasset.editor
{
    internal class DependenciesTreeView : TreeView
    {
        public readonly List<string> assetPaths = new List<string>();
        public string title;
        public bool topOnly = false;

        public DependenciesTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            Reload();
        }

        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);
            var selection = GetSelection();
            var items = Array.ConvertAll(selection.ToArray(), o => FindItem(o, rootItem));
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false,
                data =>
                {
                    var sb = new StringBuilder();
                    var assets = Array.ConvertAll(items, input => input.displayName);
                    foreach (var asset in assets)
                    {
                        sb.AppendLine($"\"{asset}\",");
                    }
                    EditorGUIUtility.systemCopyBuffer = sb.ToString();
                }, null);
            menu.ShowAsContext();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            if (string.IsNullOrEmpty(title))
            {
                title = "Dependencies";
            }

            var dependenciesItem = new TreeViewItem(title.GetHashCode(), root.depth + 1, title);
            var set = new List<string>();
            foreach (var assetPath in assetPaths)
            {
                if (topOnly)
                {
                    set.Add(assetPath);
                }
                else
                {
                    foreach (var dependency in AssetDatabase.GetDependencies(assetPath))
                    {
                        if (dependency == assetPath || Settings.ExcludeFiles.Exists(dependency.Contains) ||
                            dependency.EndsWith(".cs") || set.Contains(dependency))
                        {
                            continue;
                        }

                        set.Add(dependency);
                    }
                }
            }


            foreach (var dependency in set)
            {
                dependenciesItem.AddChild(new TreeViewItem(dependency.GetHashCode(), dependenciesItem.depth + 1,
                    dependency));
            }

            root.AddChild(dependenciesItem);
            return root;
        }
    }
}