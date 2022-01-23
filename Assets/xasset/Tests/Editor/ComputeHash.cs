using System.IO;
using NUnit.Framework;
using UnityEngine;
using xasset.editor;

namespace xasset.tests.editor
{
    public class ComputeHash
    {
        [Test]
        public void ComputeHashSimplePasses()
        {
            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
            Assert.True(versions.encryptionEnabled);
            foreach (var version in versions.data)
            {
                var manifest = Manifest.LoadFromFile(Settings.GetBuildPath(version.file));
                Assert.NotNull(manifest);
                foreach (var bundle in manifest.bundles)
                {
                    var path = Settings.GetBuildPath(bundle.nameWithAppendHash);
                    var info = new FileInfo(path);
                    if (!info.Exists)
                    {
                        continue;
                    }
                    if (bundle.isRaw)
                    {
                        continue;
                    }
                    using (var reader = new BinaryReader(File.OpenRead(path)))
                    {
                        var readHash = reader.ReadString();
                        var size = reader.ReadInt64();
                        Assert.AreEqual(size, bundle.size, bundle.nameWithAppendHash);
                        var offset = (ulong)reader.BaseStream.Position;
                        var hash = Utility.ComputeHash(reader.BaseStream);
                        Assert.AreEqual(hash, readHash, bundle.nameWithAppendHash);
                        var assetBundle = AssetBundle.LoadFromFile(path, 0, offset);
                        Assert.NotNull(assetBundle);
                        assetBundle.Unload(true);
                    }
                }
            }
        }
    }
}