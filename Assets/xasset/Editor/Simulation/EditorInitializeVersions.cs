namespace xasset.editor
{
    /// <summary>
    ///     仿真模式的初始化操作
    /// </summary>
    public class EditorInitializeVersions : Operation
    {
        public override void Start()
        {
            base.Start();
            foreach (var build in Build.GetAllBuilds())
            {
                var manifest = Manifest.LoadFromFile(build.name);
                manifest.name = build.name;
                foreach (var group in build.groups)
                {
                    Group.CollectAssets(group, (path, entry) => manifest.AddAsset(path, null)); 
                }
                Versions.LoadVersion(manifest);
            }

            Versions.ReloadPlayerVersions(null);

            Finish();
        }
    }
}