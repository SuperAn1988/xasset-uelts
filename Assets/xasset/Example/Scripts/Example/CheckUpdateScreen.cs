using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace xasset.example
{
    public class CheckUpdateScreen : MonoBehaviour
    {
        public Text version;

        private void Start()
        {
            SetVersion();
        }

        private void OnDestroy()
        {
            MessageBox.CloseAll();
        }

        public void SetVersion()
        {
            version.text = $"ver：{Versions.ManifestsVersion}";
        }

        public void ClearAll()
        {
            Versions.ClearDownload();
        }

        public void ClearHistory()
        {
            MessageBox.Show("Warning", "Are you sure to clean？", ok =>
            {
                if (ok)
                {
                    PreloadManager.Instance.ShowProgress(Versions.ClearAsync());
                }
            }, "Ensure");
        }

        public void StartCheck()
        {
            StartCoroutine(Checking());
        }

        private void GetDownloadSizeAsync()
        {
            StartCoroutine(GetDownloadSize());
        }

        private IEnumerator GetDownloadSize()
        {
            var getDownloadSize = Versions.GetDownloadSizeAsync(PreloadManager.GetPreloadAssets(ExampleScene.CheckUpdate));
            yield return getDownloadSize;
            if (getDownloadSize.downloadSize > 0)
            {
                var messageBox = PreloadManager.GetDownloadSizeMessageBox(getDownloadSize.downloadSize);
                yield return messageBox;
                if (messageBox.ok)
                {
                    PreloadManager.Instance.DownloadAsync(getDownloadSize.DownloadAsync(), OnComplete);
                    yield break;
                }
            }
            OnComplete();
        }

        private IEnumerator Checking()
        {
            PreloadManager.Instance.SetVisible(true);
            PreloadManager.Instance.SetMessage("Checking...", 0);
            var checking = Versions.CheckUpdateAsync();
            yield return checking;
            if (checking.status == OperationStatus.Failed)
            {
                MessageBox.Show("Warning", "Failed to update version information, please check the network status and try again.", ok =>
                {
                    if (ok)
                    {
                        StartCheck();
                    }
                    else
                    {
                        OnComplete();
                    }
                }, "Retry");
                yield break;
            }

            PreloadManager.Instance.SetMessage("Get the information of versions ...", 1);
            if (checking.downloadSize > 0)
            {
                var messageBox = PreloadManager.GetDownloadSizeMessageBox(checking.downloadSize);
                yield return messageBox;
                if (messageBox.ok)
                {
                    PreloadManager.Instance.DownloadAsync(checking.DownloadAsync(), GetDownloadSizeAsync);
                    yield break;
                }
            }

            GetDownloadSizeAsync();
        } 

        private void OnComplete()
        {
            SetVersion();
            PreloadManager.Instance.SetMessage("Update completed.", 1);
            PreloadManager.Instance.StartCoroutine(LoadScene());
        }

        private static IEnumerator LoadScene()
        {
            var scene = Scene.LoadAsync("Menu");
            PreloadManager.Instance.ShowProgress(scene);
            yield return scene;
        }
    }
}