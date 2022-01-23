using System;
using System.IO;
using UnityEngine;

namespace xasset.editor
{
    public static class Initializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            PathManager.PlatformName = Settings.GetPlatformName();
            var settings = Settings.GetDefaultSettings();
            var path = Settings.GetBuildPath(Versions.Filename);
            var versions = BuildVersions.Load(path);
            versions.offlineMode = settings.scriptPlayMode != ScriptPlayMode.Increment;
            File.WriteAllText(path, JsonUtility.ToJson(versions));
            switch (settings.scriptPlayMode)
            {
                case ScriptPlayMode.Simulation:
                    settings.Initialize();
                    Asset.Creator = EditorAsset.Create;
                    Scene.Creator = EditorScene.Create;
                    Versions.Initializer = () => new EditorInitializeVersions();
                    break;
                case ScriptPlayMode.Preload:
                    PathManager.PlayerDataPath = Path.Combine(Environment.CurrentDirectory, Settings.PlatformBuildPath);
                    break;
                case ScriptPlayMode.Increment:
                    if (settings.requestCopy &&
                        UnityEditor.EditorUtility.DisplayDialog("提示", "增量模式启动，是否复制资源到StreamingAssets", "复制"))
                    {
                        BuildScript.CopyToStreamingAssets();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        } 
    }
}