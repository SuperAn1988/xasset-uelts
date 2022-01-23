using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    /// <summary>
    ///     资源的打包分组。
    /// </summary>
    [CreateAssetMenu(menuName = "xasset/Group", fileName = "Group", order = 0)]
    public class Group : ScriptableObject
    {
        /// <summary>
        ///     需要采集的资源节点
        /// </summary>
        [Tooltip("需要采集的节点")] public Object[] entries;

        /// <summary>
        ///     过滤器，例如t:Scene 表示场景，t:Texture 表示纹理
        /// </summary>
        /// ManifestsWindow
        [Tooltip("过滤器，例如t:Scene 表示场景，t:Texture 表示纹理")]
        public string filter;

        /// <summary>
        ///     打包模式，控制资源的打包粒度，PackTogether 表示打包到一起，PackByFile 表示每个文件单独打包，PackByDirectory 则按目录打包
        /// </summary>
        [Tooltip("打包模式，控制资源的打包粒度，PackTogether 表示打包到一起，PackByFile 表示每个文件单独打包，PackByDirectory 则按目录打包")]
        public BundleMode bundleMode = BundleMode.PackByFile;

        public string build;

        /// <summary>
        ///     自定义打包器
        /// </summary>
        public static Func<string, string, string, string, string> customPacker { get; set; }

        /// <summary>
        ///     自定义筛选器
        /// </summary>
        public static Func<Group, string, bool> customFilter { get; set; }

        private static string GetDirectoryName(string path)
        {
            var dir = Path.GetDirectoryName(path);
            return !string.IsNullOrEmpty(dir) ? dir.Replace("\\", "/") : string.Empty;
        }

        public static string PackAsset(string assetPath, string entry, BundleMode bundleMode, string group,
            string build)
        {
            var bundle = string.Empty;
            switch (bundleMode)
            {
                case BundleMode.PackTogether:
                    bundle = group;
                    break;
                case BundleMode.PackByFolder:
                    bundle = GetDirectoryName(assetPath);
                    break;
                case BundleMode.PackByFile:
                    bundle = assetPath;
                    break;
                case BundleMode.PackByTopSubFolder:
                    bundle = PackAssetByTopDirectory(assetPath, entry, bundle);
                    break;
                case BundleMode.PackByRaw:
                    bundle = assetPath;
                    break;
                case BundleMode.PackByEntry:
                    bundle = Path.GetFileNameWithoutExtension(entry);
                    break;
                case BundleMode.PackByCustom:
                    if (customPacker == null)
                    {
                        bundle = assetPath;
                        Debug.LogWarning("没有找到实现自定义打包器，默认按文件打包");
                    }
                    else
                    {
                        bundle = customPacker?.Invoke(assetPath, bundle, group, build);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return PackAsset(assetPath, build, bundle);
        }

        public static string PackAsset(string assetPath, string build, string bundle)
        {
            if (Settings.ForceAllShadersPackTogether && assetPath.EndsWith(".shader")) bundle = "shaders";

            if (Settings.ForceAllScenesPackByFile && assetPath.EndsWith(".unity")) bundle = assetPath;

            if (Settings.AppendBuildNameToBundle) bundle = $"{build.ToLower()}_{bundle}";

            var extension = Settings.BundleExtension;
            if (Settings.ReplaceBundleNameWithHash) return Utility.ComputeHash(Encoding.UTF8.GetBytes(bundle)) + extension;

            bundle = bundle.Replace(" ", "").Replace("/", "_").Replace("-", "_").Replace(".", "_").ToLower();
            return $"{ApplyReplaceBundleNames(bundle)}{extension}";
        }

        private static string PackAssetByTopDirectory(string assetPath, string rootPath, string bundle)
        {
            if (!string.IsNullOrEmpty(rootPath))
            {
                var pos = assetPath.IndexOf("/", rootPath.Length + 1, StringComparison.Ordinal);
                bundle = pos != -1 ? assetPath.Substring(0, pos) : rootPath;
            }
            else
            {
                Debug.LogError($"invalid rootPath {assetPath}");
            }

            return bundle;
        }

        private static string ApplyReplaceBundleNames(string bundle)
        {
            foreach (var replace in Settings.ReplaceBundleNames)
            {
                if (!replace.enabled) continue;

                if (!bundle.Contains(replace.key)) continue;

                bundle = bundle.Replace(replace.key, replace.value);
                break;
            }

            return bundle;
        }


        public static void CollectAssets(Group group, Action<string, string> onCollect)
        {
            if (group == null || group.entries == null) return;

            foreach (var asset in group.entries)
            {
                if (asset == null) continue;

                var path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path)) continue;
                if (!Directory.Exists(path))
                {
                    onCollect?.Invoke(path, path);
                    continue;
                }

                var guilds = AssetDatabase.FindAssets(group.filter, new[] {path});
                foreach (var guild in guilds)
                {
                    var child = AssetDatabase.GUIDToAssetPath(guild);
                    if (string.IsNullOrEmpty(child)
                        || Directory.Exists(child)
                        || Settings.IsExcluded(child)
                        || customFilter != null && !customFilter(group, child))
                        continue;
                    onCollect?.Invoke(child, path);
                }
            }
        }

        public GroupAsset CreateAsset(string path, string entry, bool auto = false)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type != null)
                return new GroupAsset
                {
                    path = path,
                    entry = entry,
                    type = type.Name,
                    auto = auto,
                    group = this
                };
            Debug.LogWarning($"Invalid type:{path}");
            return new GroupAsset
            {
                path = path,
                entry = entry,
                type = "MissType",
                auto = auto,
                group = this
            };
        }

        public static string PackAsset(GroupAsset asset)
        {
            return PackAsset(asset.path, asset.entry, asset.group.bundleMode, asset.group.name, asset.group.build);
        }
    }
}