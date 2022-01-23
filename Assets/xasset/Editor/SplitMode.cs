namespace xasset.editor
{
    /// <summary>
    ///     分包模式
    /// </summary>
    public enum SplitMode
    {
        /// <summary>
        ///     不分包
        /// </summary>
        SplitNone,

        /// <summary>
        ///     包含
        /// </summary>
        SplitByAssetsWithDependencies,

        /// <summary>
        ///     反包含
        /// </summary>
        SplitByExcludedAssetsWithDependencies
    }
}