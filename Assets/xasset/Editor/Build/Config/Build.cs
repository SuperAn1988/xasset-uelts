using System.Collections.Generic;
using UnityEngine;

namespace xasset.editor
{
    [CreateAssetMenu(menuName = "xasset/Build", fileName = "Build", order = 0)]
    public class Build : ScriptableObject
    {
        [Tooltip("基本选项")] public BuildOptions options;
        [Tooltip("分组配置")] public List<Group> groups = new List<Group>();

        public static Build[] GetAllBuilds()
        {
            Settings.GetDefaultSettings().Initialize();
            return EditorUtility.FindAssets<Build>();
        }
    }
}