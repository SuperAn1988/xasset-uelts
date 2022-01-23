using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace xasset.example
{
    [DisallowMultipleComponent]
    public class PreloadManager : MonoBehaviour
    {
        public IProgressBar progressBar;

        public static PreloadManager Instance { get; private set; }

        public static MessageBox GetDownloadSizeMessageBox(long downloadSize)
        {
            var messageBox = MessageBox.Show("Tips", $"New content available({Utility.FormatBytes(downloadSize)})，" +
                                                     "Download now？", null, "Download", "Skip");
            return messageBox;
        }
        
        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            MessageBox.Dispose();
        }

        public void SetVisible(bool visible)
        {
            progressBar?.SetVisible(visible);
        }

        public void SetMessage(string message, float progress)
        {
            SetVisible(true);
            if (progressBar != null)
            {
                progressBar.SetMessage(message);
                progressBar.SetProgress(progress);
            }
        }

        private static string FormatBytes(long bytes)
        {
            return Utility.FormatBytes(bytes);
        }

        public void ShowProgress(Scene loading)
        {
            SetVisible(true);
            loading.completed += scene => { SetVisible(false); };
            loading.updated += scene =>
            {
                if (Download.Working)
                {
                    var current = Download.TotalDownloadedBytes;
                    var max = Download.TotalSize;
                    var speed = Download.TotalBandwidth;
                    SetSpeedMessage(current, max, speed);
                }
                else
                {
                    SetMessage($"Load the game scene without cell data... {scene.progress * 100:F2}%", scene.progress);
                }
            };
        }

        public void ShowProgress(Download downloading)
        {
            SetVisible(true);
            downloading.completed += scene => { SetVisible(false); };
            downloading.updated += scene =>
            {
                var current = Download.TotalDownloadedBytes;
                var max = Download.TotalSize;
                var speed = Download.TotalBandwidth;
                SetSpeedMessage(current, max, speed);
            };
        }

        private void SetSpeedMessage(long current, long max, long speed)
        {
            SetMessage(
                $"Loading...{FormatBytes(current)}/{FormatBytes(max)}(Speed {FormatBytes(speed)}/s)",
                current * 1f / max);
        }

        public void ShowProgress(ClearFiles clear)
        {
            SetVisible(true);
            clear.completed += o => { SetVisible(false); };
            clear.updated += (o, file) => { SetMessage($"Clean...{file}{clear.id}/{clear.max}", clear.progress); };
        }

        public void ShowProgress(DownloadFiles download)
        {
            SetVisible(true);
            download.completed += o => { SetVisible(false); };
            download.updated += o =>
            {
                var current = Download.TotalDownloadedBytes;
                var max = Download.TotalSize;
                var speed = Download.TotalBandwidth;
                SetSpeedMessage(current, max, speed);
            };
        }

        public void DownloadAsync(DownloadFiles download, Action completed)
        {
            StartCoroutine(Downloading(download, completed));
        }

        private IEnumerator Downloading(DownloadFiles download, Action completed)
        {
            ShowProgress(download);
            yield return download;
            if (download.status == OperationStatus.Failed)
            {
                var messageBox = MessageBox.Show("Warning", "Failed to update version information, please check the network status and try again.");
                yield return messageBox;
                if (messageBox.ok)
                {
                    download.Retry();
                    DownloadAsync(download, completed);
                }
                else
                {
                    Application.Quit();
                }

                yield break;
            }

            completed?.Invoke();
        }

        public static string[] GetPreloadAssets(ExampleScene scene)
        {
            var assets = new List<string>();
            switch (scene)
            {
                case ExampleScene.CheckUpdate:
                    assets.AddRange(new[]
                    {
                        "Assets/xasset/Example/Arts/Prefabs/Preload",
                        "Assets/xasset/Example/Data",
                        "OpeningDialog",
                        "CheckUpdate",
                        "Splash",
                        "Menu"
                    });
                    break;
                case ExampleScene.Additive:
                    assets.AddRange(new[]
                    {
                        "Additive2",
                        "Additive"
                    });
                    break;
                case ExampleScene.Async2Sync:
                    assets.AddRange(new[]
                    {
                        "Logo",
                        "Async2Sync"
                    });
                    break;

                case ExampleScene.CycleReferences:
                    assets.AddRange(new[]
                    {
                        "CycleReferences",
                        "Children",
                        "Children2"
                    });
                    break;

                case ExampleScene.Download:
                    assets.AddRange(new[]
                    {
                        "Download"
                    });
                    break;

                case ExampleScene.GetDownloadSize:
                    assets.AddRange(new[]
                    {
                        "GetDownloadSize"
                    });
                    break;
            }

            return assets.ToArray();
        }

        public void ShowProgress(UnpackFiles unpackAsync)
        {
            unpackAsync.updated += (message, progress) =>
            {
                SetMessage($"Unpacking...{message}", progress);
            };
            unpackAsync.completed += operation =>
            {
                SetMessage("Unpack completed.", 1);
                SetVisible(false);
            };
        }
    }
}