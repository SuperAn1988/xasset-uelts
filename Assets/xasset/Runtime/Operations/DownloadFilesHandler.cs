using System.Collections.Generic;

namespace xasset
{
    public interface IDownloadFilesHandler
    {
        void Start(DownloadFiles downloadFiles);
        void Update(DownloadFiles downloadFiles);
        void Retry(DownloadFiles downloadFiles);
    }

    public class DownloadFilesHandler : IDownloadFilesHandler
    {
        /// <summary>
        ///     已经下载的
        /// </summary>
        private readonly List<Download> _downloaded = new List<Download>();

        /// <summary>
        ///     下载失败的
        /// </summary>
        private readonly List<Download> _errors = new List<Download>();

        /// <summary>
        ///     下载中的内容
        /// </summary>
        private readonly List<Download> _progressing = new List<Download>();

        public void Start(DownloadFiles downloadFiles)
        {
            downloadFiles.downloadedBytes = 0;
            _progressing.Clear();
            _downloaded.Clear();
            foreach (var info in downloadFiles.files)
            {
                downloadFiles.totalSize += info.size;
            }

            if (downloadFiles.files.Count > 0)
            {
                foreach (var item in downloadFiles.files)
                {
                    var download = Download.DownloadAsync(item);
                    _progressing.Add(download);
                    download.retryEnabled = false;
                }
            }
            else
            {
                downloadFiles.Finish();
            }
        }

        public void Update(DownloadFiles downloadFiles)
        {
            if (downloadFiles.status != OperationStatus.Processing)
            {
                return;
            }
            if (_progressing.Count > 0)
            {
                var len = 0L;
                for (var index = 0; index < _progressing.Count; index++)
                {
                    var item = _progressing[index];
                    if (item.isDone)
                    {
                        _progressing.RemoveAt(index);
                        index--;
                        _downloaded.Add(item);
                        if (item.status == DownloadStatus.Failed)
                        {
                            _errors.Add(item);
                        }
                    }
                    else
                    {
                        len += item.downloadedBytes;
                    }
                }

                foreach (var item in _downloaded)
                {
                    len += item.downloadedBytes;
                }

                downloadFiles.downloadedBytes = len;
                downloadFiles.progress = downloadFiles.downloadedBytes * 1f / downloadFiles.totalSize;
                downloadFiles.updated?.Invoke(downloadFiles);
                return;
            }

            downloadFiles.updated = null;
            downloadFiles.Finish(_errors.Count > 0 ? "部分文件下载失败。" : null);
        }

        public void Retry(DownloadFiles downloadFiles)
        {
            downloadFiles.Start();
            foreach (var download in _errors)
            {
                Download.Retry(download);
                _progressing.Add(download);
            }
            _errors.Clear();
        }
    }

    public class WebGLDownloadFilesHandler : IDownloadFilesHandler
    {
        public static readonly Dictionary<string, Bundle> Preload = new Dictionary<string, Bundle>();

        /// <summary>
        ///     已经下载的
        /// </summary>
        private readonly List<Bundle> _downloaded = new List<Bundle>();

        /// <summary>
        ///     下载失败的
        /// </summary>
        private readonly List<Bundle> _errors = new List<Bundle>();

        /// <summary>
        ///     下载中的内容
        /// </summary>
        private readonly List<Bundle> _progressing = new List<Bundle>();

        public void Start(DownloadFiles downloadFiles)
        {
            downloadFiles.downloadedBytes = 0;
            _progressing.Clear();
            _downloaded.Clear();
            foreach (var info in downloadFiles.files)
            {
                downloadFiles.totalSize += info.size;
            }
            if (downloadFiles.files.Count > 0)
            {
                foreach (var item in downloadFiles.files)
                {
                    Logger.I($"Download {item.url}.");
                    var info = Versions.GetBundle(item.bundle);
                    if (info == null)
                    {
                        Logger.E($"(info == null {item.bundle}.");
                        continue;
                    }
                    _progressing.Add(Bundle.LoadInternal(info));
                }
            }
            else
            {
                downloadFiles.Finish();
            }
        }

        public void Update(DownloadFiles downloadFiles)
        {
            if (downloadFiles.status != OperationStatus.Processing)
            {
                return;
            }
            if (_progressing.Count > 0)
            {
                var len = 0L;
                for (var index = 0; index < _progressing.Count; index++)
                {
                    var item = _progressing[index];
                    if (item.isDone)
                    {
                        _progressing.RemoveAt(index);
                        index--;
                        _downloaded.Add(item);
                        if (!string.IsNullOrEmpty(item.error))
                        {
                            _errors.Add(item);
                        }
                    }
                }

                foreach (var item in _downloaded)
                {
                    len += item.info.size;
                }

                downloadFiles.downloadedBytes = len;
                downloadFiles.progress = downloadFiles.downloadedBytes * 1f / downloadFiles.totalSize;
                downloadFiles.updated?.Invoke(downloadFiles);
                return;
            }
            downloadFiles.updated = null;
            if (_errors.Count == 0)
            {
                foreach (var bundle in _downloaded)
                {
                    Preload[bundle.pathOrURL] = bundle;
                    Logger.I($"WebGLDownloadFilesHandler.Preload Add {bundle.pathOrURL}");
                }
                downloadFiles.Finish();
                return;
            }
            downloadFiles.Finish("部分文件下载失败。");
        }

        public void Retry(DownloadFiles downloadFiles)
        {
            downloadFiles.Start();
            foreach (var bundle in _errors)
            {
                _progressing.Add(Bundle.LoadInternal(bundle.info));
            }
            _errors.Clear();
        }
    }
}