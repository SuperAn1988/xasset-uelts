namespace xasset.editor
{
    /// <summary>
    ///     打包模式，用来控制打包粒度
    /// </summary>
    public enum BundleMode
    {
        /// <summary>
        ///     打包到一起，所有采集到的资源都按分组名字打包。
        /// </summary>
        PackTogether,

        /// <summary>
        ///     每个文件都会被打包到一个独立的包中。
        /// </summary>
        PackByFile,

        /// <summary>
        ///     按文件夹为单位打包，相同文件夹的资源打包到一起。
        /// </summary>
        PackByFolder,

        /// <summary>
        ///     按每一个 Entry 的顶层子文件夹打包，子文件夹和顶层子文件夹共享一个打包分组。
        /// </summary>
        PackByTopSubFolder,

        /// <summary>
        ///     按原始格式打包，不打包 AssetBundle，但是参与版本更新
        /// </summary>
        PackByRaw,

        /// <summary>
        ///     按 entry 的名字打包
        /// </summary>
        PackByEntry,

        /// <summary>
        ///     按自定义打包器的返回打包，如果没有实现默认按文件打包
        /// </summary>
        PackByCustom
    }
}