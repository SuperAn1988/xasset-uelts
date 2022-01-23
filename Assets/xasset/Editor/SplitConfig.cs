using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    /// <summary>
    ///     分包配置
    /// </summary>
    [CreateAssetMenu(menuName = "xasset/SplitConfig", fileName = "SplitConfig", order = 0)]
    public class SplitConfig : ScriptableObject
    {
        /// <summary>
        ///     自定义注释
        /// </summary>
        [TextArea(3, 100)] [Tooltip("自定义注释")] public string notes;

        /// <summary>
        ///     分包模式，默认是白名单，资源组中配置了的包含，黑名单则刚好相反，配置了的不采集
        /// </summary>
        [Tooltip("分包模式，默认是白名单，资源组中配置了的包含，黑名单则刚好相反，配置了的不采集")]
        public SplitMode splitMode = SplitMode.SplitByAssetsWithDependencies;

        /// <summary>
        ///     资源组
        /// </summary>
        [Tooltip("资源组")] public Object[] assets;

        public IEnumerable<string> GetAssets()
        {
            var hashset = new HashSet<string>(Array.ConvertAll(assets, AssetDatabase.GetAssetPath));
            hashset.RemoveWhere(string.IsNullOrEmpty);
            return hashset;
        }
    }
}