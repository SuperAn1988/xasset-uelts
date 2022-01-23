using System.Collections;
using UnityEngine;

namespace xasset.example
{
    public class GetDownloadSizeScreen : DownloadControl
    {
        private bool showGUI = true;

        private void OnGUI()
        {
            if (!showGUI)
            {
                return;
            }

            var rect = new Rect(Screen.width * 0.25f, Screen.height * 0.5f, Screen.width * 0.5f, Screen.height * 0.5f);
            using (new GUILayout.AreaScope(rect))
            {
                DrawSpeed();
                var count = Versions.GetVersionCount();
                if (count == 0)
                {
                    GUILayout.Label("versions not found.");
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var version = Versions.GetVersion(i);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(version.name);
                            if (GUILayout.Button("Check"))
                            {
                                StartUpdate(version.name);
                            }
                        }
                    }
                }
            }
        }

        private void StartUpdate(string manifest)
        {
            StartCoroutine(GetDownloadSizeAsync(manifest));
        }

        private IEnumerator GetDownloadSizeAsync(string manifest)
        {
            showGUI = false;
            var getDownloadSize = Versions.GetDownloadSizeAsyncWithManifest(manifest);
            yield return getDownloadSize;
            if (getDownloadSize.downloadSize > 0)
            {
                // 没有任何文件下载过，可以考虑下载 bin 文件。
                if (getDownloadSize.totalSize == getDownloadSize.downloadSize)
                {
                    var version = Versions.GetVersion(manifest);
                    if (version.binaryVersion != null)
                    {
                        var info = Versions.GetDownloadInfo(version.binaryVersion.file, version.binaryVersion.hash, version.binaryVersion.size);
                        var messageBox = PreloadManager.GetDownloadSizeMessageBox(getDownloadSize.downloadSize);
                        yield return messageBox;
                        var downloadFiles = Versions.DownloadAsync(info);
                        PreloadManager.Instance.DownloadAsync(downloadFiles, () =>
                        {
                            var unpackAsync = Versions.UnpackAsync(info.savePath);
                            PreloadManager.Instance.ShowProgress(unpackAsync);
                            unpackAsync.completed += operation =>
                            {
                                showGUI = true;
                            };
                        });
                    }
                }
                else
                {
                    var messageBox = PreloadManager.GetDownloadSizeMessageBox(getDownloadSize.downloadSize);
                    yield return messageBox;
                    if (messageBox.ok)
                    {
                        PreloadManager.Instance.ShowProgress(getDownloadSize.DownloadAsync());
                    }
                }
            }
            else
            {
                MessageBox.Show("Tips", "Nothing to download, retry?", b =>
                {
                    if (b)
                    {
                        Versions.ClearDownload();
                        StartUpdate(manifest);
                    }
                });
            }
        }
    }
}