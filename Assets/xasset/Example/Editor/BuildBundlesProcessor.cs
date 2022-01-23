using UnityEditor;
using UnityEngine;
using xasset.editor;

namespace xasset.example.editor
{
    [InitializeOnLoad]
    public static class BuildBundlesProcessor
    {
        static BuildBundlesProcessor()
        {
            BuildScript.preprocessBuildBundles += PreprocessBuildBundles;
            BuildScript.postprocessBuildBundles += PostprocessBuildBundles;
        }

        private static void PreprocessBuildBundles(BuildTask task)
        {
            Debug.LogFormat("Prepare build bundles for {0}", task.name);
        }

        private static void PostprocessBuildBundles(BuildTask task)
        {
            Settings.GetDefaultSettings().Initialize();
            Debug.LogFormat("Post build bundles for {0} with files: {1}", task.name,
                string.Join("\n", task.changes.ConvertAll(Settings.GetBuildPath)));
        }
    }
}