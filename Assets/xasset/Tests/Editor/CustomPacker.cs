using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xasset.editor;

namespace xasset.tests.editor
{
    public class CustomPacker
    {
        // A Test behaves as an ordinary method
        [Test]
        public void CustomPackerSimplePasses()
        {
            Settings.GetDefaultSettings().Initialize();
            var bundleExtension = Settings.BundleExtension;
            Group.customPacker = null;
            var build = Settings.AppendBuildNameToBundle ? "test_" : "";
            var bundle = Group.PackAsset("Assets/Prefabs/LoadingScreen.prefab", "Assets/Prefabs",
                BundleMode.PackByCustom,
                "Test", "test");
            Debug.Log(bundle);
            Assert.AreEqual($"{build}custom{bundleExtension}", bundle);
            Group.customPacker = TestCustomPacker;
            bundle = Group.PackAsset("Assets/Prefabs/LoadingScreen.prefab", "Assets/Prefabs", BundleMode.PackByCustom,
                "Test", "test");
            Debug.Log(bundle);
            Assert.AreEqual($"{build}custom{bundleExtension}", bundle);
            bundle = Group.PackAsset("Assets/Shaders/Transparent.shader", "Assets/Shaders", BundleMode.PackByCustom,
                "Test", "test");
            Debug.Log(bundle);
            Assert.AreEqual($"{build}custom{bundleExtension}", bundle);
        }

        private static string TestCustomPacker(string asset, string bundle, string group, string build)
        {
            return "custom";
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator CustomPackerWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}