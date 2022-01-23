using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    [CreateAssetMenu(menuName = "xasset/Settings", fileName = "Settings", order = 0)]
    public sealed class Settings : ScriptableObject
    {
        /// <summary>
        ///     采集资源或依赖需要过滤掉的文件
        /// </summary>
        [Header("Bundle")] [Tooltip("采集资源或依赖需要过滤掉的文件")]
        public List<string> excludeFiles =
            new List<string>
            {
                ".spriteatlas",
                ".giparams",
                "LightingData.asset"
            };

        /// <summary>
        ///     包名简化配置
        /// </summary>
        [Tooltip("包名简化配置")] public ReplaceBundleName[] replaceBundleNames;

        /// <summary>
        ///     是否将 bundle 名字进行 hash 处理，开启后，可以规避中文或一些特殊字符的平台兼容性问题。
        /// </summary>
        [Tooltip("是否将 bundle 名字进行 hash 处理，开启后，可以规避中文或一些特殊字符的平台兼容性问题。")]
        public bool replaceBundleNameWithHash;

        /// <summary>
        ///     是否将 Build 追加给 bundle
        /// </summary>
        [Tooltip("是否将 Build 追加给 bundle")] public bool appendBuildNameToBundle;

        /// <summary>
        ///     bundle 的扩展名，例如：.bundle, .unity3d, .ab, 团队版不用给 bundle 加 扩展名。
        /// </summary>
        [Tooltip("bundle 的扩展名，例如：.bundle, .unity3d, .ab, 团队版不用给 bundle 加 扩展名。")]
        public string bundleExtension;

        /// <summary>
        ///     强制所有 shader 打包到一起
        /// </summary>
        [Tooltip("强制所有 shader 打包到一起")] public bool forceAllShadersPackTogether;

        /// <summary>
        ///     强制把场景按文件打包
        /// </summary>
        [Tooltip("强制把场景按文件打包")] public bool forceAllScenesPackByFile;

        /// <summary>
        ///     安装包使用的分包配置。
        /// </summary>
        [Tooltip("安装包使用的分包配置")] public SplitConfig splitConfig;

        /// <summary>
        ///     是否开启安装包加密模式，android 平台建议开启此选项，对性能有提升。
        /// </summary>
        [Tooltip("是否开启加密模式，加密后，资源不会被轻易破解。")]
        public bool encryptionEnabled;

        /// <summary>
        ///     播放器的运行模式。Preload 模式不更新资源，并且打包的时候会忽略分包配置。
        /// </summary>
        [Tooltip("播放器的运行模式")] public ScriptPlayMode scriptPlayMode = ScriptPlayMode.Simulation;

        /// <summary>
        ///     增量模式时提示复制资源
        /// </summary>
        [Tooltip("增量模式时提示复制资源")] public bool requestCopy = true;

        public static bool ReplaceBundleNameWithHash { get; private set; }
        public static string BundleExtension { get; private set; }
        public static List<string> ExcludeFiles { get; private set; }
        public static ReplaceBundleName[] ReplaceBundleNames { get; private set; }
        public static bool AppendBuildNameToBundle { get; private set; }
        public static bool ForceAllShadersPackTogether { get; private set; }
        public static bool ForceAllScenesPackByFile { get; private set; }
        public static bool EncryptionEnabled { get; private set; }

        /// <summary>
        ///     打包输出目录
        /// </summary>
        public static string PlatformBuildPath
        {
            get
            {
                var dir = $"{Utility.buildPath}/{GetPlatformName()}";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return dir;
            }
        }

        /// <summary>
        ///     安装包资源目录, 打包安装包的时候会自动根据分包配置将资源拷贝到这个目录
        /// </summary>
        public static string BuildPlayerDataPath => $"{Application.streamingAssetsPath}/{Utility.buildPath}";

        public void Initialize()
        {
            BundleExtension = bundleExtension;
            ReplaceBundleNameWithHash = replaceBundleNameWithHash;
            ExcludeFiles = excludeFiles;
            ReplaceBundleNames = replaceBundleNames;
            AppendBuildNameToBundle = appendBuildNameToBundle;
            ForceAllShadersPackTogether = forceAllShadersPackTogether;
            ForceAllScenesPackByFile = forceAllScenesPackByFile;
            EncryptionEnabled = encryptionEnabled;
        }

        public static Settings GetDefaultSettings()
        {
            return EditorUtility.FindOrCreateAsset<Settings>("Assets/xasset/Settings.asset");
        }

        /// <summary>
        ///     获取包含在安装包的资源
        /// </summary>
        /// <returns></returns>
        public List<ManifestBundle> GetBundlesInBuild(BuildVersions versions)
        {
            var bundles = new List<ManifestBundle>();
            foreach (var version in versions.data)
            {
                var manifest = Manifest.LoadFromFile(GetBuildPath(version.file));
                if (splitConfig != null && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                {
                    if (splitConfig.splitMode == SplitMode.SplitNone)
                    {
                        bundles.AddRange(manifest.bundles);
                    }
                    else
                    {
                        foreach (var asset in CollectAssets(splitConfig, manifest))
                        {
                            var bundle = manifest.GetBundle(asset);
                            if (bundle == null)
                            {
                                continue;
                            }

                            if (!bundles.Contains(bundle))
                            {
                                bundles.Add(bundle);
                            }

                            foreach (var dependency in manifest.GetDependencies(bundle))
                            {
                                if (!bundles.Contains(dependency))
                                {
                                    bundles.Add(dependency);
                                }
                            }
                        }
                    }
                }
                else
                {
                    bundles.AddRange(manifest.bundles);
                }
            }

            return bundles;
        }

        private static IEnumerable<string> CollectAssets(SplitConfig config, Manifest manifest)
        {
            var assets = new HashSet<string>();
            if (config.splitMode == SplitMode.SplitByAssetsWithDependencies)
            {
                foreach (var item in config.GetAssets())
                {
                    if (manifest.IsDirectory(item))
                    {
                        assets.UnionWith(manifest.GetAssetsWithDirectory(item, true));
                    }
                    else
                    {
                        assets.Add(item);
                    }
                }
            }
            else if (config.splitMode == SplitMode.SplitByExcludedAssetsWithDependencies)
            {
                assets.UnionWith(manifest.GetAssets());
                foreach (var item in config.GetAssets())
                {
                    if (manifest.IsDirectory(item))
                    {
                        assets.ExceptWith(manifest.GetAssetsWithDirectory(item, true));
                    }
                    else
                    {
                        assets.Remove(item);
                    }
                }
            }
            return assets;
        }

        public static string GetBuildPath(string file)
        {
            return $"{PlatformBuildPath}/{file}";
        }

        public static string GetPlatformName()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                default:
                    return Utility.nonsupport;
            }
        }

        public static bool IsExcluded(string path)
        {
            return ExcludeFiles.Exists(path.EndsWith) || path.EndsWith(".cs") || path.EndsWith(".dll");
        }

        public static IEnumerable<string> GetDependencies(string path)
        {
            var set = new HashSet<string>(AssetDatabase.GetDependencies(path, true));
            set.Remove(path);
            set.RemoveWhere(IsExcluded);
            return set.ToArray();
        }

        public void Save()
        {
            EditorUtility.SaveAsset(this);
        }
    }
}