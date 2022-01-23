using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    /// <summary>
    ///     异步获取下载大小。
    /// </summary>
    public sealed class GetDownloadSize : Operation
    {
        public readonly List<ManifestBundle> bundles = new List<ManifestBundle>();
        public readonly List<DownloadInfo> changes = new List<DownloadInfo>();

        private readonly HashSet<string> savedBundles = new HashSet<string>();

        private Func<bool> processor;

        /// <summary>
        ///     需要下载的大小
        /// </summary>
        public long downloadSize { get; private set; }

        /// <summary>
        ///     文件的总大小
        /// </summary>
        public long totalSize { get; private set; }

        public override void Start()
        {
            base.Start();
            downloadSize = 0;
            if (bundles.Count == 0)
            {
                Finish();
            }
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                processor = ProcessingWithWebGL;
            }
            else
            {
                if (Versions.OfflineMode)
                {
                    Finish();
                    return;
                }
                processor = ProcessingWithoutWebGL;
            }
        }

        public DownloadFiles DownloadAsync()
        {
            var downloadFiles = Versions.DownloadAsync(changes.ToArray());
            return downloadFiles;
        }

        protected override void Update()
        {
            if (status != OperationStatus.Processing)
            {
                return;
            }

            if (processor())
            {
                return;
            }

            Finish();
        }

        private bool ProcessingWithWebGL()
        {
            while (bundles.Count > 0)
            {
                var bundle = bundles[0];
                var url = PathManager.GetPlayerDataPath(bundle.nameWithAppendHash);
                if (!WebGLDownloadFilesHandler.Preload.ContainsKey(url))
                {
                    var info = Versions.GetDownloadInfo(bundle.nameWithAppendHash, bundle.hash, bundle.size);
                    info.url = url;
                    info.bundle = bundle.name;
                    downloadSize += bundle.size;
                    changes.Add(info);
                }
                savedBundles.Add(url);
                totalSize += bundle.size;
                bundles.RemoveAt(0);
                if (Updater.busy)
                {
                    return true;
                }
            }
            var unused = new List<string>();
            foreach (var pair in WebGLDownloadFilesHandler.Preload)
            {
                if (savedBundles.Contains(pair.Key))
                {
                    continue;
                }
                unused.Add(pair.Key);
            }
            foreach (var key in unused)
            {
                if (WebGLDownloadFilesHandler.Preload.TryGetValue(key, out var value))
                {
                    value.Release();
                    WebGLDownloadFilesHandler.Preload.Remove(key);
                    Logger.I($"WebGLDownloadFilesHandler.Preload Remove {key}");
                }
            }
            unused.Clear();
            return false;
        }

        private bool ProcessingWithoutWebGL()
        {
            while (bundles.Count > 0)
            {
                var bundle = bundles[0];
                var savePath = Downloader.GetDownloadDataPath(bundle.nameWithAppendHash);
                if (!Versions.IsDownloaded(bundle) && !changes.Exists(info => info.savePath == savePath))
                {
                    var info = Versions.GetDownloadInfo(bundle.nameWithAppendHash, bundle.hash, bundle.size);
                    info.encryption = Versions.EncryptionEnabled && !bundle.isRaw;
                    info.bundle = bundle.name;
                    downloadSize += info.downloadSize;
                    changes.Add(info);
                }

                totalSize += bundle.size;
                bundles.RemoveAt(0);
                if (Updater.busy)
                {
                    return true;
                }
            }
            return false;
        }
    }
}