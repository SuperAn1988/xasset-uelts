namespace xasset.editor
{
    public class RebaseManifest : BuildTaskJob
    {
        private readonly Manifest _manifest;

        public RebaseManifest(BuildTask task, Manifest manifest) : base(task)
        {
            _manifest = manifest;
        }

        public override void Run()
        {
            var versions = BuildVersions.Load(GetBuildPath(Versions.Filename));
            var version = versions.Get(_task.name);
            var manifest = Manifest.LoadFromFile(GetBuildPath(version?.file));
            _manifest.version = manifest.version + 1;
            _task.SaveManifest(_manifest);
        }
    }
}