using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using xasset.editor;

namespace xasset.tests.editor
{
    public class CustomFilter
    {
        // A Test behaves as an ordinary method
        [Test]
        public void CustomFilterSimplePasses()
        {
            Group.customFilter += TestCustomFilter;
            var groups = EditorUtility.FindAssets<Group>();
            var assets = new List<string>();
            foreach (var item in groups)
            { 
                Group.CollectAssets(item, (path, entry) =>
                {
                    assets.Add(path);
                } );
            }

            Assert.True(assets.TrueForAll(o => !o.Contains("Logo")));
            Group.customFilter = null;
            assets.Clear();
            foreach (var item in groups)
            {
                Group.CollectAssets(item, (path, entry) =>
                {
                    assets.Add(path);
                } );
            }

            Assert.True(assets.Exists(o => o.Contains("Logo")));
        }

        private static bool TestCustomFilter(Group group, string arg)
        {
            if (arg.Contains("Logo"))
            {
                return false;
            }

            return true;
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator CustomFilterWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}