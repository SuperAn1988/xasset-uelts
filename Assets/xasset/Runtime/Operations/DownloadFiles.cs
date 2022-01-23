using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    /// <summary>
    ///     批量下载文件的的操作，下载失败后，可以通过 <see cref="Retry" /> 方法对失败的内容重启下载。
    /// </summary>
    public sealed class DownloadFiles : Operation
    {
        /// <summary>
        ///     需要下载的信息
        /// </summary>
        public readonly List<DownloadInfo> files = new List<DownloadInfo>();

        private IDownloadFilesHandler downloadFilesHandler;

        /// <summary>
        ///     更新时触发
        /// </summary>
        public Action<DownloadFiles> updated;

        /// <summary>
        ///     总下载大小
        /// </summary>
        public long totalSize { get; internal set; }

        /// <summary>
        ///     已经下载了的大小
        /// </summary>
        public long downloadedBytes { get; internal set; }

        public override void Start()
        {
            base.Start();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                downloadFilesHandler = new WebGLDownloadFilesHandler();
            }
            else
            {
                downloadFilesHandler = new DownloadFilesHandler();
            }
            downloadFilesHandler.Start(this);
        }

        protected override void Update()
        {
            downloadFilesHandler.Update(this);
        }

        public void Retry()
        {
            downloadFilesHandler.Retry(this);
        }
    }
}