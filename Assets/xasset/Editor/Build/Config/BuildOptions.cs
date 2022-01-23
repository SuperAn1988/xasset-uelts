using System;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    [Serializable]
    public class BuildOptions
    {
        /// <summary>
        ///     是否开启自动分组，带依赖的资源开启自动分组可以快速优化冗余
        /// </summary>
        [Tooltip("是否开启自动分组，带依赖的资源开启自动分组可以快速优化冗余")]
        public bool autoGroupWithReference = true;

        /// <summary>
        ///     是否将打包后的文件合并生成二进制
        /// </summary>
        [Tooltip("是否将打包后的文件合并生成二进制")] public bool packAllBundlesToBinary;

        /// <summary>
        ///     AssetBundles 的打包选项
        /// </summary>
        [Tooltip("AssetBundles 的打包选项")]
        public BuildAssetBundleOptions buildAssetBundleOptions = BuildAssetBundleOptions.ChunkBasedCompression;
    }
}