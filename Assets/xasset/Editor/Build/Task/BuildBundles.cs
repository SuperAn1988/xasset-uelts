using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace xasset.editor
{
    public class BuildBundles : BuildTaskJob
    {
        private readonly BuildAssetBundleOptions _options;
        public readonly List<ManifestBundle> bundles = new List<ManifestBundle>();

        public BuildBundles(BuildTask task, BuildAssetBundleOptions options) : base(task)
        {
            _options = options;
        }

        private ABuildPipeline BuildPipeline => customBuildPipeline != null ? customBuildPipeline.Invoke(this) : new BuiltinBuildPipeline();

        public static Func<BuildBundles, ABuildPipeline> customBuildPipeline { get; set; }

        public override void Run()
        {
            CreateBundles();
            if (bundles.Count > 0)
            {
                if (!BuildAssetBundles())
                {
                    return;
                }
            }
            BuildRawAssetsToBundles();
            _task.bundles.AddRange(bundles);
        }

        private void BuildRawAssetsToBundles()
        {
            var rawAssets = _task.rawAssets;
            if (rawAssets.Count == 0)
            {
                return;
            }
            foreach (var asset in rawAssets)
            {
                if (string.IsNullOrEmpty(asset.path))
                {
                    Logger.E("RawAsset not found:{0}", asset.path);
                    continue;
                }
                var file = new FileInfo(asset.path);
                var crc = Utility.ComputeHash(asset.path);
                var nameWithAppendHash = $"{Path.GetFileNameWithoutExtension(asset.bundle)}_{crc}{Settings.BundleExtension}";
                var bundle = new ManifestBundle
                {
                    hash = crc,
                    name = asset.bundle,
                    nameWithAppendHash = nameWithAppendHash,
                    isRaw = true,
                    assets = new List<string>
                    {
                        asset.path
                    },
                    size = file.Length
                };
                var path = _task.outputPath + "/" + bundle.nameWithAppendHash;
                if (!File.Exists(path))
                {
                    file.CopyTo(path);
                }
                bundles.Add(bundle);
            }
        }

        protected AssetBundleBuild[] GetBuilds()
        {
            return bundles.ConvertAll(bundle =>
                new AssetBundleBuild
                {
                    assetNames = bundle.assets.ToArray(),
                    assetBundleName = bundle.name
                }).ToArray();
        }

        private bool BuildAssetBundles()
        {
            var manifest = BuildPipeline.BuildAssetBundles(_task.outputPath, GetBuilds(), _options, EditorUserBuildSettings.activeBuildTarget);
            if (manifest == null)
            {
                TreatError($"Failed to build AssetBundles with {_task.name}.");
                return false;
            }
            var nameWithBundles = GetBundles();
            if (Settings.EncryptionEnabled && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                if (!BuildWithEncryption(nameWithBundles, manifest))
                {
                    return false;
                }
            }
            else
            {
                if (!BuildWithoutEncryption(nameWithBundles, manifest))
                {
                    return false;
                }
            }

            return true;
        }

        private bool BuildWithoutEncryption(Dictionary<string, ManifestBundle> nameWithBundles, IAssetBundleManifest manifest)
        {
            var assetBundles = manifest.GetAllAssetBundles();
            foreach (var assetBundle in assetBundles)
            {
                if (nameWithBundles.TryGetValue(assetBundle, out var bundle))
                {
                    var path = GetBuildPath(assetBundle);
                    var hash = Utility.ComputeHash(path);
                    var nameWithAppendHash = $"{Path.GetFileNameWithoutExtension(path)}_{hash}{Settings.BundleExtension}";
                    bundle.hash = hash;
                    bundle.deps = Array.ConvertAll(manifest.GetAllDependencies(assetBundle), input => nameWithBundles[input].id);
                    bundle.nameWithAppendHash = nameWithAppendHash;
                    var dir = Path.GetDirectoryName(path);
                    var newPath = $"{dir}/{nameWithAppendHash}";
                    var info = new FileInfo(path);
                    if (info.Exists)
                    {
                        bundle.size = info.Length;
                    }
                    else
                    {
                        TreatError($"File not found: {info}");
                        return false;
                    }
                    if (!File.Exists(newPath))
                    {
                        info.CopyTo(newPath, true);
                    }
                }
                else
                {
                    TreatError($"Bundle not found: {assetBundle}");
                    return false;
                }
            }
            return true;
        }

        private bool BuildWithEncryption(Dictionary<string, ManifestBundle> nameWithBundles, IAssetBundleManifest manifest)
        {
            var assetBundles = manifest.GetAllAssetBundles();
            foreach (var assetBundle in assetBundles)
            {
                if (nameWithBundles.TryGetValue(assetBundle, out var bundle))
                {
                    bundle.hash = manifest.GetAssetBundleHash(assetBundle);
                    bundle.deps = Array.ConvertAll(manifest.GetAllDependencies(assetBundle), input => nameWithBundles[input].id);
                    var path = GetBuildPath(assetBundle);
                    var info = new FileInfo(path);
                    if (info.Exists)
                    {
                        var dir = Path.GetDirectoryName(path);
                        var nameWithAppendHash = $"{Path.GetFileNameWithoutExtension(info.FullName)}_{bundle.hash}{Settings.BundleExtension}";
                        bundle.nameWithAppendHash = nameWithAppendHash;
                        var newPath = $"{dir}/{nameWithAppendHash}";
                        var newInfo = new FileInfo(newPath);
                        if (!newInfo.Exists)
                        {
                            var hash = Utility.ComputeHash(path);
                            using (Stream stream = File.OpenRead(path))
                            {
                                using (var writer = new BinaryWriter(File.OpenWrite(newPath)))
                                {
                                    writer.Write(hash);
                                    bundle.size = writer.BaseStream.Position + stream.Length + sizeof(long);
                                    writer.Write(bundle.size);
                                    writer.Write(File.ReadAllBytes(path));
                                }
                            }
                        }
                        else
                        {
                            bundle.size = newInfo.Length;
                        }
                    }
                    else
                    {
                        TreatError($"File not found: {info}");
                        return false;
                    }
                }
                else
                {
                    TreatError($"Bundle not found: {assetBundle}");
                    return false;
                }
            }
            return true;
        }

        private Dictionary<string, ManifestBundle> GetBundles()
        {
            var nameWithBundles = new Dictionary<string, ManifestBundle>();
            for (var i = 0; i < bundles.Count; i++)
            {
                var bundle = bundles[i];
                bundle.id = i;
                nameWithBundles[bundle.name] = bundle;
            }
            return nameWithBundles;
        }

        private void CreateBundles()
        {
            var dictionary = new Dictionary<string, List<string>>();
            foreach (var asset in _task.bundledAssets)
            {
                if (!dictionary.TryGetValue(asset.bundle, out var assets))
                {
                    assets = new List<string>();
                    dictionary.Add(asset.bundle, assets);
                    bundles.Add(new ManifestBundle
                    {
                        name = asset.bundle,
                        assets = assets
                    });
                }
                assets.Add(asset.path);
            }
        }
    }
}