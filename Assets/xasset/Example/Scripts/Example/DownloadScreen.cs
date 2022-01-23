using System.IO;
using UnityEngine;

namespace xasset.example
{
    public class DownloadScreen : DownloadControl
    {
        public string url = "https://xasset.pro/video/example.mp4";
        private Download _download;

        private void OnGUI()
        {
            var rect = new Rect(Screen.width * 0.25f, Screen.height * 0.5f, Screen.width * 0.5f, Screen.height * 0.5f);
            using (new GUILayout.AreaScope(rect))
            {
                DrawSpeed();
                DrawDownload();
            }
        }

        private void DrawDownload()
        {
            using (new GUILayout.HorizontalScope())
            {
                url = GUILayout.TextField(url);
                if (GUILayout.Button("Download"))
                {
                    StartDownload();
                }

                if (GUILayout.Button("Retry"))
                {
                    StartDownload(true);
                }

                if (_download != null && !_download.isDone)
                {
                    if (GUILayout.Button(_download.status == DownloadStatus.Progressing ? "Pause" : "Resume"))
                    {
                        if (_download.status == DownloadStatus.Progressing)
                        {
                            _download.Pause();
                        }
                        else
                        {
                            _download.UnPause();
                        }
                    }

                    if (GUILayout.Button("Cancel"))
                    {
                        _download.Cancel();
                    }
                }
            }
        }

        private void Completed(Download download)
        {
            if (download.status == DownloadStatus.Failed)
            {
                MessageBox.Show("Warning", "Failed to update version information, please check the network status and try again.", isOk =>
                {
                    if (isOk)
                    {
                        StartDownload();
                    }
                });
            }
        }

        public void StartDownload(bool delete = false)
        {
            var savePath = Downloader.GetDownloadDataPath(Path.GetFileName(url));
            if (File.Exists(savePath) && delete)
            {
                File.Delete(savePath);
            }

            _download = Download.DownloadAsync(url, savePath, Completed);
            PreloadManager.Instance.ShowProgress(_download);
        }
    }
}