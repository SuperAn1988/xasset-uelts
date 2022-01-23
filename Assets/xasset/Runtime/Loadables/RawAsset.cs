using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace xasset
{
    public sealed class RawAsset : Loadable
    {
        public static readonly Dictionary<string, RawAsset> Cache = new Dictionary<string, RawAsset>();
        private ManifestBundle _info;
        private UnityWebRequest _request;
        public Action<RawAsset> completed;
        public string savePath { get; private set; }

        public Task<RawAsset> Task
        {
            get
            {
                var tcs = new TaskCompletionSource<RawAsset>();
                completed += operation => { tcs.SetResult(this); };
                return tcs.Task;
            }
        }

        public override void LoadImmediate()
        {
            if (isDone)
            {
                return;
            }
            while (!_request.isDone)
            {
            }
            Finish(_request.error);
        }

        protected override void OnLoad()
        {
            _info = Versions.GetBundle(pathOrURL);
            if (_info == null)
            {
                Finish("File not found.");
                return;
            }

            if (Versions.SimulationMode)
            {
                savePath = pathOrURL;
                Finish();
                return;
            }

            savePath = Downloader.GetDownloadDataPath(_info.nameWithAppendHash);
            var file = new FileInfo(savePath);
            if (file.Exists)
            {
                if (file.Length == _info.size)
                {
                    Finish();
                    return;
                }
                File.Delete(savePath);
            }
            var url = Versions.IsDownloaded(_info) ? PathManager.GetPlayerDataURL(_info.nameWithAppendHash) : Downloader.GetDownloadDataPath(_info.nameWithAppendHash);
            _request = UnityWebRequest.Get(url);
            _request.downloadHandler = new DownloadHandlerFile(savePath);
            _request.SendWebRequest();
            status = LoadableStatus.Loading;
        }

        protected override void OnUnload()
        {
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }

            Cache.Remove(pathOrURL);
        }

        protected override void OnComplete()
        {
            if (completed == null)
            {
                return;
            }

            var saved = completed;
            completed?.Invoke(this);
            completed -= saved;
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading)
            {
                return;
            }
            UpdateLoading();
        }

        protected override void OnUnused()
        {
            completed = null;
        }

        private void UpdateLoading()
        {
            if (_request == null)
            {
                Finish("request == null");
                return;
            }

            if (!_request.isDone)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_request.error))
            {
                Finish(_request.error);
                return;
            }

            Finish();
        }

        public static RawAsset LoadAsync(string filename)
        {
            return LoadInternal(filename);
        }

        public static RawAsset Load(string filename)
        {
            return LoadInternal(filename, true);
        }

        private static RawAsset LoadInternal(string filename, bool mustCompleteOnNextFrame = false)
        {
            PathManager.GetActualPath(ref filename);
            if (!Versions.Contains(filename))
            {
                throw new FileLoadException(filename);
            }
            if (!Cache.TryGetValue(filename, out var asset))
            {
                asset = new RawAsset
                {
                    pathOrURL = filename
                };
                Cache.Add(filename, asset);
            }
            asset.Load();
            if (mustCompleteOnNextFrame)
            {
                asset.LoadImmediate();
            }
            return asset;
        }

        public static string GetSavePath(string filename)
        {
            var bundle = Versions.GetBundle(filename);
            return bundle == null ? null : Downloader.GetDownloadDataPath(filename);
        }
    }
}