using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    public class Bundle : Loadable
    {
        public static readonly Dictionary<string, Bundle> Cache = new Dictionary<string, Bundle>();

        private static readonly Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();

        public ManifestBundle info;
        public static Func<string, ManifestBundle, Bundle> customLoader { get; set; }

        public AssetBundle assetBundle { get; protected set; }

        protected AssetBundleCreateRequest LoadAssetBundleAsync(string url)
        {
            return AssetBundle.LoadFromFileAsync(url, 0, Versions.GetOffset(info, url));
        }

        protected AssetBundle LoadAssetBundle(string url)
        {
            return AssetBundle.LoadFromFile(url, 0, Versions.GetOffset(info, url));
        }

        protected void OnLoaded(AssetBundle bundle)
        {
            assetBundle = bundle;
            Finish(assetBundle == null ? "assetBundle == null" : null);
            AssetBundles[info.name] = bundle;
        }

        internal static Bundle LoadInternal(ManifestBundle bundle)
        {
            if (bundle == null)
            {
                throw new NullReferenceException();
            }

            if (!Cache.TryGetValue(bundle.nameWithAppendHash, out var item))
            {
                if (AssetBundles.TryGetValue(bundle.name, out var assetBundle))
                {
                    // 这里要防止外部把 bundle 给释放了。
                    if (assetBundle != null)
                    {
                        assetBundle.Unload(false);
                    }

                    AssetBundles.Remove(bundle.name);
                }

                var url = PathManager.GetBundlePathOrURL(bundle);
                if (customLoader != null)
                {
                    item = customLoader(url, bundle);
                }

                if (item == null)
                {
                    if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("ftp://"))
                    {
                        item = new DownloadBundle { pathOrURL = url, info = bundle };
                    }
                    else
                    {
                        item = new LocalBundle { pathOrURL = url, info = bundle };
                    }
                }

                Cache.Add(bundle.nameWithAppendHash, item);
            }
            item.Load();
            return item;
        }

        protected override void OnUnload()
        {
            Cache.Remove(info.nameWithAppendHash);
            if (assetBundle == null)
            {
                return;
            }

            assetBundle.Unload(true);
            assetBundle = null;
            AssetBundles.Remove(info.name);
        }
    }
}