using System;

namespace xasset.editor
{
    [Serializable]
    public class GroupAsset
    {
        public string path;
        public string bundle;
        public string type;
        public string entry;
        public bool auto;

        /// <summary>
        ///     是否分析依赖，纹理，文本等不带依赖的资源关闭这个选项可以加快打包速度，带依赖的资源开启可以让依赖参与自动分组，快速优化打包冗余问题
        /// </summary>
        public bool findReferences => !type.Contains("TextAsset") && !type.Contains("Texture");

        public Group group { get; set; }
    }
}