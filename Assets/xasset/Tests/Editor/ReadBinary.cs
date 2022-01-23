using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using xasset.editor;

namespace xasset.tests.editor
{
    public class ReadBinary
    {
        public static string GetRelativePath(string filename)
        {
            return $"{Settings.BuildPlayerDataPath}/{filename}";
        }

        [Test]
        public void ReadBinarySimplePasses()
        {
            var path = GetRelativePath(Versions.EncryptionData);
            Assert.NotNull(path);
            var versions = BuildVersions.Load(GetRelativePath(Versions.Filename));
            var locations = new Dictionary<string, AssetLocation>();
            foreach (var location in versions.streamingAssets)
            {
                locations[location.name] = location;
            }

            AssetBundle.UnloadAllAssetBundles(true);
            foreach (var version in versions.data)
            {
                var manifest = Manifest.LoadFromFile(GetRelativePath(version.file));
                foreach (var bundle in manifest.bundles)
                {
                    if (!bundle.isRaw && locations.TryGetValue(bundle.nameWithAppendHash, out var value))
                    {
                        var assetBundle = AssetBundle.LoadFromFile(path, 0, value.offset);
                        Assert.NotNull(assetBundle);
                    }
                }
            }
            AssetBundle.UnloadAllAssetBundles(true);
        }
    }
}