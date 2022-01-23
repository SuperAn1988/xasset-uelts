using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace xasset
{
    public enum VerifyMode
    {
        Size,
        Hash
    }

    /// <summary>
    ///     Versions 类，持有运行时的所有资源的版本信息和依赖关系。
    /// </summary>
    public static class Versions
    {
        public const string APIVersion = "2022.1.1p1";
        public const string EncryptionData = "data.bin";
        public const string Filename = "versions.json";
        internal static readonly List<Manifest> Manifests = new List<Manifest>();
        internal static readonly Dictionary<string, Manifest> NameWithManifests = new Dictionary<string, Manifest>();

        internal static readonly Dictionary<string, AssetLocation> StreamingAssets = new Dictionary<string, AssetLocation>();

        public static VerifyMode VerifyMode { get; set; } = VerifyMode.Size;

        /// <summary>
        ///     是否是仿真模式
        /// </summary>
        public static bool SimulationMode { get; private set; }

        /// <summary>
        ///     是否是加密模式
        /// </summary>
        public static bool EncryptionEnabled { get; set; }

        /// <summary>
        ///     是否是离线模式
        /// </summary>
        public static bool OfflineMode { get; private set; }

        /// <summary>
        ///     获取清单的版本号
        /// </summary>
        public static string ManifestsVersion
        {
            get
            {
                var sb = new StringBuilder();
                for (var index = 0; index < Manifests.Count; index++)
                {
                    var manifest = Manifests[index];
                    sb.Append(manifest.version);
                    if (index < Manifests.Count - 1)
                    {
                        sb.Append(".");
                    }
                }

                return sb.ToString();
            }
        }

        public static Func<Operation> Initializer { get; set; } = () => new InitializeVersions();

        public static bool Initialized { get; internal set; }
        public static bool UseBinary { get; internal set; }

        /// <summary>
        ///     获取下载信息。
        /// </summary>
        /// <param name="file">指定的文件名</param>
        /// <param name="hash">指定的文件哈希</param>
        /// <param name="size">指定文件的下载大小</param>
        /// <param name="fastVerify"></param>
        /// <returns></returns>
        public static DownloadInfo GetDownloadInfo(string file, string hash, long size, bool fastVerify = true)
        {
            if (VerifyMode == VerifyMode.Size && fastVerify)
            {
                hash = null;
            }

            var info = new DownloadInfo
            {
                hash = hash,
                size = size,
                savePath = Downloader.GetDownloadDataPath(file),
                url = Downloader.GetDownloadURL(file)
            };
            return info;
        }

        /// <summary>
        ///     加载安装包的版本文件。
        /// </summary>
        /// <param name="versions">安装包的版本文件</param>
        public static void ReloadPlayerVersions(BuildVersions versions)
        {
            StreamingAssets.Clear();
            // 版本数据为空的时候，是仿真模式。
            if (versions == null)
            {
                SimulationMode = true;
                OfflineMode = true;
                return;
            }

            foreach (var asset in versions.streamingAssets)
            {
                StreamingAssets[asset.name] = asset;
            }

            EncryptionEnabled = versions.encryptionEnabled;
            OfflineMode = versions.offlineMode;
            UseBinary = versions.binaryEnabled;
            SimulationMode = false;
        }

        public static bool Changed(BuildVersion version)
        {
            return !NameWithManifests.TryGetValue(version.name, out var value) ||
                   value.nameWithAppendHash != version.file;
        }

        public static bool Exist(BuildVersion version)
        {
            if (version == null)
            {
                return false;
            }

            var info = new FileInfo(Downloader.GetDownloadDataPath(version.file));
            return info.Exists
                   && info.Length == version.size
                   && VerifyMode == VerifyMode.Size
                   || Utility.ComputeHash(info.FullName) == version.hash;
        }

        public static void LoadVersion(BuildVersion version)
        {
            var path = Downloader.GetDownloadDataPath(version.file);
            var manifest = Manifest.LoadFromFile(path);
            manifest.name = version.name;
            Logger.I("LoadVersion:{0} with file {1}.", version.name, path);
            manifest.nameWithAppendHash = version.file;
            LoadVersion(manifest);
        }

        public static void LoadVersion(Manifest manifest)
        {
            var key = manifest.name;
            if (NameWithManifests.TryGetValue(key, out var value))
            {
                value.Copy(manifest);
                return;
            }

            Manifests.Add(manifest);
            NameWithManifests.Add(key, manifest);
        }

        public static Manifest GetVersion(string name)
        {
            return NameWithManifests.TryGetValue(name, out var value) ? value : null;
        }

        public static int GetVersionCount()
        {
            return Manifests.Count;
        }

        public static Manifest GetVersion(int index)
        {
            return index >= 0 && index < Manifests.Count ? Manifests[index] : null;
        }

        /// <summary>
        ///     清理版本数据，不传参数等于清理不在当前版本的所有历史数据，传参数表示清理指定资源和依赖。
        /// </summary>
        /// <returns></returns>
        public static ClearFiles ClearAsync(params string[] files)
        {
            var clearAsync = new ClearFiles();
            if (files.Length == 0)
            {
                if (Directory.Exists(Downloader.DownloadDataPath))
                {
                    clearAsync.files.AddRange(Directory.GetFiles(Downloader.DownloadDataPath));
                    var usedFiles = new HashSet<string>();
                    foreach (var manifest in Manifests)
                    {
                        usedFiles.Add(manifest.nameWithAppendHash);
                        if (manifest.binaryVersion != null)
                        {
                            usedFiles.Add(manifest.binaryVersion.file);
                        }
                        foreach (var bundle in manifest.bundles)
                        {
                            usedFiles.Add(bundle.nameWithAppendHash);
                        }
                    }

                    clearAsync.files.RemoveAll(file =>
                    {
                        var name = Path.GetFileName(file);
                        return usedFiles.Contains(name);
                    });
                }
            }
            else
            {
                var assets = new HashSet<string>();
                foreach (var file in files)
                {
                    if (!GetDependencies(file, out var bundle, out var deps))
                    {
                        continue;
                    }

                    assets.Add(Downloader.GetDownloadDataPath(bundle.nameWithAppendHash));
                    foreach (var dep in deps)
                    {
                        assets.Add(Downloader.GetDownloadDataPath(dep.nameWithAppendHash));
                    }
                }

                clearAsync.files.AddRange(assets);
            }

            clearAsync.Start();
            return clearAsync;
        }

        /// <summary>
        ///     清理所有下载数据
        /// </summary>
        public static void ClearDownload()
        {
            PathManager.BundleWithPathOrUrLs.Clear();
            if (Directory.Exists(Downloader.DownloadDataPath))
            {
                Directory.Delete(Downloader.DownloadDataPath, true);
            }
        }

        /// <summary>
        ///     初始化，会根据 versions.json 文件加载清单。
        /// </summary>
        /// <returns></returns>
        public static Operation InitializeAsync()
        {
            var operation = Initializer();
            operation.Start();
            operation.completed += o =>
            {
                Initialized = true;
            };
            return operation;
        }

        /// <summary>
        ///     检查版本更新的操作
        /// </summary>
        /// <param name="url">versions.json 的地址 不传默认为下载地址 + versions.json, 生成环境中，这里可以改成带版本号的版本，例如 DownloadURL/v1/versions.json。 </param>
        /// <param name="hash">versions.json 的 hash</param>
        /// <returns></returns>
        public static CheckUpdate CheckUpdateAsync(string url = null, string hash = null)
        {
            var operation = new CheckUpdate { url = url, hash = hash };
            operation.Start();
            return operation;
        }

        /// <summary>
        ///     根据资源名称获取更新大小
        /// </summary>
        /// <param name="assetNames">资源名称，可以是加载的相对路径，也可以是不带hash的bundle名字</param>
        /// <returns></returns>
        public static GetDownloadSize GetDownloadSizeAsync(params string[] assetNames)
        {
            var getDownloadSize = new GetDownloadSize();
            getDownloadSize.bundles.AddRange(GetBundlesWithAssets(Manifests, assetNames));
            getDownloadSize.Start();
            return getDownloadSize;
        }

        /// <summary>
        ///     获取一个清单的更新大小。
        /// </summary>
        /// <param name="manifest">清单名字（不带hash）</param>
        /// <returns></returns>
        public static GetDownloadSize GetDownloadSizeAsyncWithManifest(string manifest)
        {
            var getDownloadSize = new GetDownloadSize();
            if (NameWithManifests.TryGetValue(manifest, out var value))
            {
                getDownloadSize.bundles.AddRange(value.bundles);
            }
            getDownloadSize.Start();
            return getDownloadSize;
        }

        /// <summary>
        ///     批量下载指定集合的内容。
        /// </summary>
        /// <param name="items">要下载内容</param>
        /// <returns></returns>
        public static DownloadFiles DownloadAsync(params DownloadInfo[] items)
        {
            var download = new DownloadFiles();
            download.files.AddRange(items);
            download.Start();
            return download;
        }

        /// <summary>
        ///     解压二进制文件
        /// </summary>
        /// <param name="name">文件名</param>
        /// <returns></returns>
        public static UnpackFiles UnpackAsync(string name)
        {
            var unpack = new UnpackFiles
            {
                name = name
            };
            unpack.Start();
            return unpack;
        }

        /// <summary>
        ///     判断 bundle 是否已经下载
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="checkStreamingAssets"></param>
        /// <returns></returns>
        public static bool IsDownloaded(ManifestBundle bundle, bool checkStreamingAssets = true)
        {
            if (bundle == null)
            {
                return false;
            }
            if (OfflineMode || checkStreamingAssets && StreamingAssets.ContainsKey(bundle.nameWithAppendHash))
            {
                return true;
            }
            var path = Downloader.GetDownloadDataPath(bundle.nameWithAppendHash);
            var file = new FileInfo(path);
            if (!file.Exists || file.Length < bundle.size)
            {
                return false;
            }
            if (file.Length == bundle.size && VerifyMode == VerifyMode.Size)
            {
                return true;
            }
            if (bundle.isRaw || !EncryptionEnabled)
            {
                return bundle.hash == Utility.ComputeHash(path);
            }
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                var hash = reader.ReadString();
                var size = reader.ReadInt64();
                if (size < file.Length)
                {
                    return false;
                }
                var stream = reader.BaseStream;
                return Utility.ComputeHash(stream) == hash;
            }
        }

        /// <summary>
        ///     判断加载路径对应的 bundle 是否下载
        /// </summary>
        /// <param name="path">加载路径</param>
        /// <param name="checkDependencies">是否统计依赖</param>
        /// <returns></returns>
        public static bool IsDownloaded(string path, bool checkDependencies)
        {
            if (!checkDependencies || !GetDependencies(path, out var bundle, out var dependencies))
            {
                return IsDownloaded(GetBundle(path));
            }

            foreach (var dependency in dependencies)
            {
                if (!IsDownloaded(dependency))
                {
                    return false;
                }
            }

            return IsDownloaded(bundle);
        }

        /// <summary>
        ///     获取指定资源的依赖
        /// </summary>
        /// <param name="assetPath">加载路径</param>
        /// <param name="mainBundle">主 bundle</param>
        /// <param name="dependencies">依赖的 bundle 集合</param>
        /// <returns></returns>
        public static bool GetDependencies(string assetPath, out ManifestBundle mainBundle,
            out ManifestBundle[] dependencies)
        {
            foreach (var manifest in Manifests)
            {
                if (manifest.Contains(assetPath))
                {
                    mainBundle = manifest.GetBundle(assetPath);
                    dependencies = manifest.GetDependencies(mainBundle);
                    return true;
                }
            }

            mainBundle = null;
            dependencies = null;
            return false;
        }

        /// <summary>
        ///     判断资源是否包含在当前版本中。
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static bool Contains(string assetPath)
        {
            foreach (var manifest in Manifests)
            {
                if (manifest.Contains(assetPath))
                {
                    return true;
                }
            }

            return false;
        }

        public static ManifestBundle GetBundle(string bundle)
        {
            foreach (var manifest in Manifests)
            {
                var getBundle = manifest.GetBundle(bundle);
                if (getBundle != null)
                {
                    return getBundle;
                }
            }

            return null;
        }

        public static ulong GetOffset(ManifestBundle bundle, string path)
        {
            if (!EncryptionEnabled)
            {
                return 0;
            }
            if (StreamingAssets.TryGetValue(bundle.nameWithAppendHash, out var value))
            {
                Logger.I("Read {0},{1},{2}", bundle.nameWithAppendHash, bundle.size, bundle.hash);
                return value.offset;
            }
            if (!File.Exists(path))
            {
                return 0;
            }
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                var hash = reader.ReadString();
                var size = reader.ReadInt64();
                bundle.offset = (ulong)reader.BaseStream.Position;
                Logger.I("Read {0},{1},{2}", bundle.nameWithAppendHash, size, hash);
                return bundle.offset;
            }
        }

        public static AssetLocation GetAssetLocation(string assetName)
        {
            return StreamingAssets.TryGetValue(assetName, out var value) ? value : null;
        }

        private static bool GetBundles(ICollection<ManifestBundle> bundles, Manifest manifest, string assetPath)
        {
            var bundle = manifest.GetBundle(assetPath);
            if (bundle == null)
            {
                return false;
            }

            if (!bundles.Contains(bundle))
            {
                bundles.Add(bundle);
            }

            foreach (var dependency in manifest.GetDependencies(bundle))
            {
                if (StreamingAssets.ContainsKey(dependency.nameWithAppendHash) ||
                    bundles.Contains(dependency))
                {
                    continue;
                }

                bundles.Add(dependency);
            }

            return true;
        }

        public static string[] GetDependencies(string path)
        {
            foreach (var manifest in Manifests)
            {
                if (manifest.Contains(path))
                {
                    return manifest.GetDependencies(path);
                }
            }
            return Array.Empty<string>();
        }

        private static IEnumerable<ManifestBundle> GetBundlesWithAssets(IEnumerable<Manifest> manifests,
            ICollection<string> assets)
        {
            var bundles = new List<ManifestBundle>();
            if (assets != null && assets.Count != 0)
            {
                foreach (var manifest in manifests)
                foreach (var asset in assets)
                {
                    if (manifest.IsDirectory(asset))
                    {
                        var children = manifest.GetAssetsWithDirectory(asset, true);
                        foreach (var child in children)
                        {
                            if (!GetBundles(bundles, manifest, child))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        var actualPath = asset;
                        PathManager.GetActualPath(ref actualPath);
                        if (!GetBundles(bundles, manifest, actualPath))
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (var manifest in manifests)
                {
                    bundles.AddRange(manifest.bundles);
                }
            }

            return bundles;
        }

        public static string[] GetAssetsWithDirectory(string dir, bool recursion)
        {
            foreach (var manifest in Manifests)
            {
                if (manifest.IsDirectory(dir))
                {
                    return manifest.GetAssetsWithDirectory(dir, recursion);
                }
            }

            return Array.Empty<string>();
        }

        public static bool IsStreamingAsset(string name)
        {
            return StreamingAssets.ContainsKey(name);
        }

        public static bool IsRawAsset(string path)
        {
            foreach (var manifest in Manifests)
            {
                if (manifest.IsRawAsset(path))
                {
                    return true;
                }
            }
            return false;
        }
    }
}