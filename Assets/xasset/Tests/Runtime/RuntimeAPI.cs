using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace xasset.tests
{
    public class RuntimeAPI
    {
        [UnityTest]
        public IEnumerator InitializeAsync()
        {
            var initialize = Versions.InitializeAsync();
            yield return initialize;
            Assert.AreEqual(initialize.status, OperationStatus.Success);
            Download.FtpPassword = string.Empty;
            Download.FtpUserID = string.Empty;
            Updater.maxUpdateTimeSlice = float.MaxValue;
            Bundle.customLoader = null;
        }

        [UnityTest]
        public IEnumerator LoadAssetWithSubAssetsAsync()
        {
            yield return InitializeAsync();
            const string path = "Assets/xasset/Example/Arts/Textures/igg.png";
            var asset = Asset.LoadWithSubAssetsAsync(path, typeof(Sprite));
            asset.completed += a =>
            {
                Debug.Log($"Completed:{a.pathOrURL}");
                Assert.NotNull(a.subAssets);
                a.Release();
            };
            asset = Asset.LoadWithSubAssets(path, typeof(Sprite));
            Assert.NotNull(asset.subAssets);
            asset.Release();
            yield return new WaitForSeconds(3);
        }

        [UnityTest]
        public IEnumerator ClearAsync()
        {
            yield return InitializeAsync();
            var clear = Versions.ClearAsync();
            Assert.True(!clear.isDone);
            yield return clear;
            Assert.True(clear.isDone);
            Assert.True(string.IsNullOrEmpty(clear.error));
        }

        [UnityTest]
        public IEnumerator UpdateAsync()
        {
            yield return InitializeAsync();
            Versions.ClearDownload();
            Downloader.DownloadURL = "http://127.0.0.1/Bundles/";
            var check = Versions.CheckUpdateAsync();
            yield return check;
            if (check.downloadSize > 0)
            {
                yield return check.DownloadAsync();
            }
            var getDownloadSize = Versions.GetDownloadSizeAsync();
            yield return getDownloadSize;
            yield return getDownloadSize.DownloadAsync();
        }

        [UnityTest]
        public IEnumerator LoadAssetFromAsyncToSync()
        {
            yield return InitializeAsync();
            for (var i = 0; i < 3; i++)
            {
                var asset = Asset.LoadAsync("Assets/Versions.Example/Prefabs/Children2.prefab", typeof(GameObject));
                asset.completed += a =>
                {
                    Debug.Log($"Completed:{a.pathOrURL}");
                    Assert.NotNull(a.asset);
                };
                asset.Release();
                asset = Asset.Load("Assets/Versions.Example/Prefabs/Children2.prefab", typeof(GameObject));
                Assert.NotNull(asset.asset);
                asset.Release();
            }
            yield return new WaitUntil(() => Loadable.Unused.Count == 0 && Loadable.Loading.Count == 0);
        }

        [UnityTest]
        public IEnumerator LoadScene()
        {
            yield return InitializeAsync();
            var scene = Scene.LoadAsync("Assets/Versions.Example/Scenes/Welcome.unity");
            scene.completed += s =>
            {
                Debug.Log($"Completed:{s.pathOrURL}");
            };
            scene.onload += o => { o.allowSceneActivation = false; };
            yield return scene;
            scene.load.allowSceneActivation = true;
            yield return new WaitForSeconds(3);
        }
    }
}