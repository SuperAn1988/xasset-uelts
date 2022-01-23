using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace xasset
{
    public class Loadable
    {
        private static readonly Dictionary<string, int> _Loads = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> _Unloads = new Dictionary<string, int>();


        public static readonly List<Loadable> Loading = new List<Loadable>();
        public static readonly List<Loadable> Unused = new List<Loadable>();

        protected static bool _updateUnloadUnusedAssets;

        private readonly Reference _reference = new Reference();
        private int _startFrame;
        private float _startTime;

        public int loads => GetTimes(pathOrURL, _Loads);
        public int unloads => GetTimes(pathOrURL, _Unloads);
        public static Action<Loadable> onLoad { get; set; }
        public static Action<Loadable> onUnloaded { get; set; }
        public LoadableStatus status { get; protected set; } = LoadableStatus.Wait;
        public string pathOrURL { get; protected set; }
        public string error { get; internal set; }

        public bool isDone => status == LoadableStatus.SuccessToLoad || status == LoadableStatus.Unloaded ||
                              status == LoadableStatus.FailedToLoad;

        public float progress { get; protected set; }
        public float elapsed { get; private set; }

        public int frames { get; private set; }

        public string loadScene { get; private set; }

        public string unloadScene { get; private set; }

        public int referenceCount => _reference.count;

        private static int GetTimes(string path, IReadOnlyDictionary<string, int> times)
        {
            return times.TryGetValue(path, out var value) ? value : 0;
        }

        [Conditional("DEBUG")]
        private static void AddTimes(Loadable obj, IDictionary<string, int> dict)
        {
            if (!dict.TryGetValue(obj.pathOrURL, out var times))
            {
                dict[obj.pathOrURL] = 1;
            }
            else
            {
                dict[obj.pathOrURL] = times + 1;
            }
        }

        protected void Finish(string errorCode = null)
        {
            error = errorCode;
            status = string.IsNullOrEmpty(errorCode) ? LoadableStatus.SuccessToLoad : LoadableStatus.FailedToLoad;
            progress = 1;
        }

        public static void UpdateAll()
        {
            for (var index = 0; index < Loading.Count; index++)
            {
                var item = Loading[index];
                if (Updater.busy)
                {
                    return;
                }

                item.Update();
                if (!item.isDone)
                {
                    continue;
                }

                Loading.RemoveAt(index);
                index--;
                item.Complete();
            }

            if (Scene.IsLoadingOrUnloading())
            {
                return;
            }

            for (int index = 0, max = Unused.Count; index < max; index++)
            {
                var item = Unused[index];
                if (Updater.busy)
                {
                    break;
                }

                if (!item.isDone)
                {
                    continue;
                }

                Unused.RemoveAt(index);
                index--;
                max--;
                if (!item._reference.unused)
                {
                    continue;
                }

                item.Unload();
            }

            if (Unused.Count > 0)
            {
                return;
            }

            if (_updateUnloadUnusedAssets)
            {
                Resources.UnloadUnusedAssets();
                _updateUnloadUnusedAssets = false;
            }
        }

        private void Update()
        {
            OnUpdate();
        }

        private void Complete()
        {
            if (status == LoadableStatus.FailedToLoad)
            {
                Logger.E("Unable to load {0} {1} with error: {2}", GetType().Name, pathOrURL, error);
                Release();
            }

            if (elapsed == 0)
            {
                elapsed = (Time.realtimeSinceStartup - _startTime) * 1000;
                frames = Time.frameCount - _startFrame;
            }

            OnComplete();
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnLoad()
        {
        }

        protected virtual void OnUnload()
        {
        }

        protected virtual void OnComplete()
        {
        }

        public virtual void LoadImmediate()
        {
            throw new InvalidOperationException();
        }

        protected void Load()
        {
            if (status != LoadableStatus.Wait && _reference.unused)
            {
                Unused.Remove(this);
            }

            _reference.Retain();
            Loading.Add(this);
            if (status != LoadableStatus.Wait)
            {
                return;
            }

            loadScene = SceneManager.GetActiveScene().name;
            onLoad?.Invoke(this);
            AddTimes(this, _Loads);
            Logger.I("Load {0} {1}.", GetType().Name, pathOrURL);
            status = LoadableStatus.Loading;
            progress = 0;
            _startTime = Time.realtimeSinceStartup;
            _startFrame = Time.frameCount;
            OnLoad();
        }

        private void Unload()
        {
            if (status == LoadableStatus.Unloaded)
            {
                return;
            }

            unloadScene = SceneManager.GetActiveScene().name;
            onUnloaded?.Invoke(this);
            AddTimes(this, _Unloads);
            Logger.I("Unload {0} {1}.", GetType().Name, pathOrURL, error);
            OnUnload();
            status = LoadableStatus.Unloaded;
        }

        public void Release()
        {
            if (_reference.count <= 0)
            {
                Logger.W("Release {0} {1}.", GetType().Name, Path.GetFileName(pathOrURL));
                return;
            }

            _reference.Release();
            if (!_reference.unused)
            {
                return;
            }

            Unused.Add(this);
            OnUnused();
        }

        protected virtual void OnUnused()
        {
        }

        public static void ClearAll()
        {
            Asset.Cache.Clear();
            Bundle.Cache.Clear();
            Dependencies.Cache.Clear();
            RawAsset.Cache.Clear();
            AssetBundle.UnloadAllAssetBundles(true);
        }
    }

    public enum LoadableStatus
    {
        Wait,
        Loading,
        DependentLoading,
        SuccessToLoad,
        FailedToLoad,
        Unloaded,
        Downloading
    }
}