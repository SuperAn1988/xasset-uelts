using System.Collections.Generic;
using System.IO;
using UnityEditor.IMGUI.Controls;

namespace xasset.editor
{
    internal class FilesTreeView : TreeView
    {
        private readonly List<string> files = new List<string>();

        public FilesTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            Reload();
        }

        public void SetFiles(IEnumerable<string> newFiles)
        {
            files.Clear();
            files.AddRange(newFiles);
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var item = new TreeViewItem("Changes".GetHashCode(), root.depth + 1, "Changes");
            foreach (var file in files)
            {
                var path = Settings.GetBuildPath(file);
                var info = new FileInfo(path);
                item.AddChild(new TreeViewItem(file.GetHashCode(), item.depth + 1,
                    $"{file}({Utility.FormatBytes(info.Exists ? info.Length : 0)})"));
            }

            root.AddChild(item);
            return root;
        }
    }
}