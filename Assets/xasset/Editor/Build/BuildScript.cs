using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    public static class BuildScript
    {
        public static Action<BuildTask> postprocessBuildBundles { get; set; }
        public static Action<BuildTask> preprocessBuildBundles { get; set; }

        public static void BuildBundles(BuildTask task)
        {
            preprocessBuildBundles?.Invoke(task);
            task.Run();
            postprocessBuildBundles?.Invoke(task);
        }

        public static void BuildBundles()
        {
            var tasks = new List<string>();
            var builds = Build.GetAllBuilds();
            foreach (var build in builds)
            {
                tasks.Add(AssetDatabase.GetAssetPath(build));
            }
            BuildBundles(tasks);
        }

        private static void BuildBundles(List<string> tasks)
        {
            foreach (var task in tasks)
            {
                BuildBundles(new BuildTask(AssetDatabase.LoadAssetAtPath<Build>(task)));
            }
        }

        public static string GetTimeForNow()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private static string GetBuildTargetName(BuildTarget target)
        {
            var targetName = $"/{PlayerSettings.productName}-v{PlayerSettings.bundleVersion}-{GetTimeForNow()}";
            switch (target)
            {
                case BuildTarget.Android:
                    return targetName + ".apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return targetName + ".exe";
                case BuildTarget.StandaloneOSX:
                    return targetName + ".app";
                default:
                    return targetName;
            }
        }

        public static void BuildPlayer(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = $"Players/{Settings.GetPlatformName()}";
            }

            var levels = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    levels.Add(scene.path);
                }
            }

            if (levels.Count == 0)
            {
                Debug.Log("Nothing to build.");
                return;
            }

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetName = GetBuildTargetName(buildTarget);
            if (buildTargetName == null)
            {
                return;
            }

            if (buildTarget == BuildTarget.WebGL)
            {
                var settings = Settings.GetDefaultSettings();
                settings.scriptPlayMode = ScriptPlayMode.Preload;
                settings.Save();
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = levels.ToArray(),
                locationPathName = path + buildTargetName,
                target = buildTarget,
                options = EditorUserBuildSettings.development
                    ? UnityEditor.BuildOptions.Development
                    : UnityEditor.BuildOptions.None
            };
            BuildPipeline.BuildPlayer(buildPlayerOptions);
            UnityEditor.EditorUtility.OpenWithDefaultApp(path);
        }

        public static void CopyToStreamingAssets(params BuildRecord[] records)
        {
            var settings = Settings.GetDefaultSettings();
            var destinationDir = Settings.BuildPlayerDataPath;
            if (Directory.Exists(destinationDir))
            {
                Directory.Delete(destinationDir, true);
            }

            Directory.CreateDirectory(destinationDir);
            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
            var timestamp = DateTime.Now.ToFileTime();
            foreach (var item in records)
            {
                var path = Settings.GetBuildPath(item.file);
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    continue;
                }
                if (timestamp > item.timestamp)
                {
                    timestamp = item.timestamp;
                }
                versions.Set(item.build, item.file, info.Length, timestamp, Utility.ComputeHash(path));
            }

            var bundles = settings.GetBundlesInBuild(versions);
            if (!settings.encryptionEnabled)
            {
                foreach (var bundle in bundles)
                {
                    Copy(bundle.nameWithAppendHash, destinationDir);
                }
            }
            else
            {
                // iOS 合并后对性能有影响不合并
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                {
                    CreateBinary(bundles, Path.Combine(Settings.BuildPlayerDataPath, Versions.EncryptionData));
                    versions.binaryEnabled = true;
                }
                else
                {
                    foreach (var bundle in bundles)
                    {
                        var path = Settings.GetBuildPath(bundle.nameWithAppendHash);
                        var newPath = $"{destinationDir}/{bundle.nameWithAppendHash}";
                        using (var writer = new BinaryWriter(File.OpenWrite(newPath)))
                        {
                            Write(path, writer, bundle);
                        }
                    }
                }
            }

            foreach (var build in versions.data)
            {
                Copy(build.file, destinationDir);
            }

            versions.streamingAssets = bundles.ConvertAll(o => new AssetLocation
            {
                name = o.nameWithAppendHash,
                offset = o.offset
            });

            versions.encryptionEnabled = settings.encryptionEnabled;
            versions.offlineMode = settings.scriptPlayMode != ScriptPlayMode.Increment;
            File.WriteAllText($"{destinationDir}/{Versions.Filename}", JsonUtility.ToJson(versions));
        }

        public static void CreateBinary(List<ManifestBundle> bundles, string savePath)
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            using (var stream = File.Open(savePath, FileMode.CreateNew, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var bundle in bundles)
                    {
                        var assetPath = Settings.GetBuildPath(bundle.nameWithAppendHash);
                        if (!File.Exists(assetPath))
                        {
                            Debug.LogWarningFormat("Bundle not found: {0}", bundle.name);
                            continue;
                        }
                        Write(assetPath, writer, bundle);
                    }
                }
            }
        }

        private static void Write(string assetPath, BinaryWriter writer, ManifestBundle bundle)
        {
            using (var reader = new BinaryReader(File.OpenRead(assetPath)))
            {
                writer.Write(reader.ReadString()); // hash 
                writer.Write(reader.ReadInt64()); // size
                bundle.offset = (ulong)writer.BaseStream.Position;
                var size = reader.BaseStream.Length - reader.BaseStream.Position;
                writer.Write(reader.ReadBytes((int)size));
            }
        }

        private static void Copy(string filename, string destinationDir)
        {
            var from = Settings.GetBuildPath(filename);
            if (File.Exists(from))
            {
                var dest = $"{destinationDir}/{filename}";
                File.Copy(from, dest, true);
            }
            else
            {
                Debug.LogErrorFormat("File not found: {0}", from);
            }
        }

        public static void ClearBuildFromSelection()
        {
            var filtered = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);
            var assetPaths = new List<string>();
            foreach (var o in filtered)
            {
                var assetPath = AssetDatabase.GetAssetPath(o);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                assetPaths.Add(assetPath);
            }

            var bundles = new List<string>();
            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
            foreach (var version in versions.data)
            {
                var manifest = Manifest.LoadFromFile(Settings.GetBuildPath(version.file));
                foreach (var assetPath in assetPaths)
                {
                    var bundle = manifest.GetBundle(assetPath);
                    if (bundle != null)
                    {
                        bundles.Add(bundle.nameWithAppendHash);
                    }
                }
            }

            foreach (var bundle in bundles)
            {
                var file = Settings.GetBuildPath(bundle);
                if (!File.Exists(file))
                {
                    continue;
                }

                File.Delete(file);
                Debug.LogFormat("Delete:{0}", file);
            }
        }

        public static void ClearHistory()
        {
            var usedFiles = new List<string>
            {
                Settings.GetPlatformName(),
                Settings.GetPlatformName() + ".manifest",
                Versions.Filename
            };

            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
            foreach (var version in versions.data)
            {
                usedFiles.Add(version.file);
                var manifest = Manifest.LoadFromFile(Settings.GetBuildPath(version.file));
                if (manifest.binaryVersion != null)
                {
                    usedFiles.Add(manifest.binaryVersion.file);
                }
                foreach (var bundle in manifest.bundles)
                {
                    usedFiles.Add(bundle.name);
                    usedFiles.Add($"{bundle.name}.manifest");
                    usedFiles.Add(bundle.nameWithAppendHash);
                }
            }

            var files = Directory.GetFiles(Settings.PlatformBuildPath);
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (usedFiles.Contains(name))
                {
                    continue;
                }

                File.Delete(file);
                Debug.LogFormat("Delete {0}", file);
            }
        }

        public static void BuildBundlesWithSelection()
        {
            var tasks = new List<string>();
            var builds = Selection.GetFiltered<Build>(SelectionMode.DeepAssets);
            foreach (var build in builds)
            {
                tasks.Add(AssetDatabase.GetAssetPath(build));
            }
            BuildBundles(tasks);
        }

        public static void FindReferences()
        {
            var builds = Build.GetAllBuilds();
            var assetWithBuilds = new Dictionary<string, List<string>>();
            foreach (var build in builds)
            {
                var autoAssets = new HashSet<string>();
                var allAssets = new List<GroupAsset>();
                foreach (var item in build.groups)
                {
                    item.build = build.name;
                    Group.CollectAssets(item, (path, entry) =>
                    {
                        var asset = item.CreateAsset(path, entry);
                        asset.bundle = Group.PackAsset(asset);
                        allAssets.Add(asset);
                        AddAsset(assetWithBuilds, asset.path, build.name, item.name);
                        if (asset.findReferences)
                        {
                            autoAssets.UnionWith(Settings.GetDependencies(asset.path));
                        }
                    }); 
                }

                autoAssets.ExceptWith(allAssets.ConvertAll(a => a.path));
                foreach (var asset in autoAssets)
                {
                    AddAsset(assetWithBuilds, asset, build.name, "Auto");
                }
            }

            var sb = new StringBuilder();
            foreach (var pair in assetWithBuilds.Where(pair => pair.Value.Count > 1))
            {
                sb.AppendLine(pair.Key);
                foreach (var s in pair.Value)
                {
                    sb.AppendLine(" - " + s);
                }
            }

            var content = sb.ToString();
            if (string.IsNullOrEmpty(content))
            {
                if (UnityEditor.EditorUtility.DisplayDialog("提示", "检查完毕，暂无异常！", "确定"))
                {
                }

                return;
            }

            const string filename = "MultipleReferences.txt";
            File.WriteAllText(filename, content);
            if (UnityEditor.EditorUtility.DisplayDialog("提示", "检测到多重引用关系，是否打开查看？", "确定"))
            {
                UnityEditor.EditorUtility.OpenWithDefaultApp(filename);
            }
        }

        private static void AddAsset(IDictionary<string, List<string>> assetWithBuilds, string asset, string buildName,
            string groupName)
        {
            if (!assetWithBuilds.TryGetValue(asset, out var value))
            {
                value = new List<string>();
                assetWithBuilds[asset] = value;
            }

            value.Add(buildName + "|" + groupName);
        }

        public static void ClearBuild()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("提示", "清理构建数据将无法正常增量打包，确认清理？", "确定"))
            {
                return;
            }

            var buildPath = Settings.PlatformBuildPath;
            Directory.Delete(buildPath, true);
            var records = BuildRecords.GetRecords();
            records.Clear();
            records.Save();
        }
    }
}