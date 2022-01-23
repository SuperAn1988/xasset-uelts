namespace xasset.editor
{
    public interface IDependenciesEditor
    {
        void ReloadDependencies(params string[] dependencies);
    }
}