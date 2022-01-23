using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace xasset.tests.editor
{
    public class CustomDownloader
    {
        // A Test behaves as an ordinary method
        [Test]
        public void CustomDownloadURLSimplePasses()
        {
            Downloader.DownloadURL = "http://127.0.0.1/Bundles";
            Downloader.CustomDownloader += TestCustomDownloader;
            var url = Downloader.GetDownloadURL("Arts");
            Assert.AreEqual($"{Downloader.DownloadURL}/Arts_hash", url);
            url = Downloader.GetDownloadURL("Data");
            Assert.AreEqual($"{Downloader.DownloadURL}/Data", url);
        }

        private static string TestCustomDownloader(string arg)
        {
            return arg.Contains("Arts") ? $"{Downloader.DownloadURL}/{arg}_hash" : $"{Downloader.DownloadURL}/{arg}";
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator CustomDownloadURLWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}