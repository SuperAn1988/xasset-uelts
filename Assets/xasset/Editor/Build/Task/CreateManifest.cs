using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace xasset.editor
{
    public class CreateManifest : BuildTaskJob
    {
        private readonly bool _packAllBundlesToBinary;

        public CreateManifest(BuildTask task, bool packAllBundlesToBinary) : base(task)
        {
            _packAllBundlesToBinary = packAllBundlesToBinary;
        }

        public override void Run()
        {
            var forceRebuild = _task.forceRebuild;
            var buildNumber = _task.buildVersion;
            var versions = BuildVersions.Load(GetBuildPath(Versions.Filename));
            if (Settings.EncryptionEnabled != versions.encryptionEnabled)
            {
                forceRebuild = true;
            }
            var version = versions.Get(_task.name);
            var manifest = Manifest.LoadFromFile(GetBuildPath(version?.file));
            if (buildNumber > 0)
            {
                manifest.version = buildNumber;
            }
            else
            {
                manifest.version++;
            }
            _task.buildVersion = manifest.version;
            var getBundles = new Dictionary<string, ManifestBundle>();
            foreach (var bundle in manifest.bundles)
            {
                getBundles[bundle.name] = bundle;
            }

            var dirs = new List<string>();
            var assets = new List<ManifestAsset>();
            var manifestAssets = new Dictionary<string, ManifestAsset>();
            var bundles = _task.bundles;

            for (var index = 0; index < bundles.Count; index++)
            {
                var bundle = bundles[index];
                foreach (var asset in bundle.assets)
                {
                    var dir = Path.GetDirectoryName(asset)?.Replace("\\", "/");
                    var pos = dirs.IndexOf(dir);
                    if (pos == -1)
                    {
                        pos = dirs.Count;
                        dirs.Add(dir);
                    }

                    var manifestAsset = new ManifestAsset
                    {
                        name = Path.GetFileName(asset),
                        bundle = index,
                        dir = pos,
                        id = assets.Count
                    };
                    assets.Add(manifestAsset);
                    manifestAssets.Add(asset, manifestAsset);
                }
                if (getBundles.TryGetValue(bundle.name, out var value) && value.hash == bundle.hash)
                {
                    continue;
                }
                changes.Add(bundle.nameWithAppendHash);
            }

            if (changes.Count == 0 && !forceRebuild)
            {
                error = "Nothing to build.";
                Debug.LogWarning(error);
                return;
            }

            GetDependencies(manifestAssets);
            manifest.bundleExtension = Settings.BundleExtension;
            manifest.bundles = bundles;
            manifest.assets = assets;
            manifest.dirs = dirs;

            if (_packAllBundlesToBinary)
            {
                PackBinary(manifest);
            }
            else
            {
                manifest.binaryVersion = null;
            }

            _task.changes.AddRange(changes);
            _task.SaveManifest(manifest);
        }

        private void PackBinary(Manifest manifest)
        {
            var taskNameToLowered = _task.name.ToLower();
            var filename = $"{taskNameToLowered}.bin";
            var path = GetBuildPath(filename);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using (var stream = File.Open(path, FileMode.CreateNew, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var bundle in manifest.bundles)
                    {
                        var assetPath = GetBuildPath(bundle.nameWithAppendHash);
                        if (string.IsNullOrEmpty(bundle.nameWithAppendHash) || !File.Exists(assetPath))
                        {
                            continue;
                        }

                        var bytes = File.ReadAllBytes(assetPath);
                        writer.Write(bundle.name);
                        writer.Write(bundle.nameWithAppendHash);
                        writer.Write(bundle.hash);
                        writer.Write(bundle.size);
                        writer.Write(bytes);
                    }
                }
            }
            var hash = Utility.ComputeHash(path);
            var nameWithAppendHash = $"{taskNameToLowered}_v{manifest.version}_{hash}.bin";
            var toPath = GetBuildPath(nameWithAppendHash);
            var size = new FileInfo(path).Length;
            File.Copy(path, toPath, true);
            File.Delete(path);
            changes.Add(nameWithAppendHash);
            var buildVersion = new BuildVersion
            {
                file = nameWithAppendHash,
                name = filename,
                hash = hash,
                size = size
            };
            manifest.binaryVersion = buildVersion;
        }

        private static void GetDependencies(Dictionary<string, ManifestAsset> assets)
        {
            foreach (var pair in assets)
            {
                var asset = pair.Value;
                var deps = new HashSet<int>();
                var dependencies = Settings.GetDependencies(pair.Key);
                foreach (var dependency in dependencies)
                {
                    if (assets.TryGetValue(dependency, out var value))
                    {
                        deps.Add(value.id);
                    }
                }

                asset.deps = deps.ToArray();
            }
        }
    }
}