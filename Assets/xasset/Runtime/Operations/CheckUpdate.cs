using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace xasset
{
    /// <summary>
    ///     检查版本更新，先下载 versions.json 文件，同步后根据改文件记录的版本信息更新和下载清单文件。
    /// </summary>
    public sealed class CheckUpdate : Operation
    {
        private readonly List<BuildVersion> _changes = new List<BuildVersion>();
        private Download _download;
        public string hash;
        public string url;

        /// <summary>
        ///     需要更新的大小
        /// </summary>
        public long downloadSize { get; private set; }

        /// <summary>
        ///     下载更新的内容。
        /// </summary>
        /// <returns></returns>
        public DownloadFiles DownloadAsync()
        {
            var infos = _changes.ConvertAll(input => Versions.GetDownloadInfo(input.file, input.hash, input.size));
            var downloadFiles = Versions.DownloadAsync(infos.ToArray());
            downloadFiles.completed = operation => { ApplyChanges(); };
            return downloadFiles;
        }

        /// <summary>
        ///     加载下载后的版本文件。
        /// </summary>
        private void ApplyChanges()
        {
            foreach (var version in _changes)
            {
                // 加载没有加载且已经下载到本地的版本文件。
                if (Versions.Changed(version) && Versions.Exist(version))
                {
                    Versions.LoadVersion(version);
                }
            }

            _changes.Clear();
        }

        public override void Start()
        {
            base.Start();
            if (Versions.OfflineMode || Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Finish();
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                url = Downloader.GetDownloadURL(Versions.Filename);
            }
            var savePath = Downloader.GetDownloadDataPath(Versions.Filename);
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            
            _download = Download.DownloadAsync(url, savePath, null, 0, hash);
        }

        protected override void Update()
        {
            if (status != OperationStatus.Processing)
            {
                return;
            }

            if (!_download.isDone)
            {
                return;
            }

            if (_download.status == DownloadStatus.Success)
            {
                downloadSize = 0;
                var versions = BuildVersions.Load(_download.info.savePath);
                foreach (var item in versions.data)
                {
                    if (Versions.Exist(item))
                    {
                        continue;
                    }

                    downloadSize += item.size;
                    _changes.Add(item);
                }

                Finish();
            }
            else
            {
                Finish(_download.error);
            }
        }
    }
}