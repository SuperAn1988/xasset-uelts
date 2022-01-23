using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    /// <summary>
    ///     命令行打包工具
    /// </summary>
    public static class CommandLine
    {
        /// <summary>
        ///     创建命令行打包脚本
        /// </summary>
        /// <param name="script"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public static void CreateTools(string script, string method, string args)
        {
            // TODO: 这里如果是 Mac 平台，applicationPath 指向的是 .app 文件夹，需要指向可执行文件。
            var cmd =
                $"\"{EditorApplication.applicationPath}\" -quit -batchmode -logfile BuildBundles.log -projectPath \"{Environment.CurrentDirectory}\" -executeMethod {script}.{method} {args}";
            File.WriteAllText(method + ".bat", cmd);
            File.WriteAllText(method + ".sh", cmd);
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        /// <summary>
        ///     打包资源
        /// </summary>
        public static void BuildBundles()
        {
            var build = GetArg("-build");
            Debug.LogFormat("CommandLine.BuildBundles {0}", build);
            var version = GetArg("-version");
            if (!int.TryParse(version, out var buildVersion))
            {
                buildVersion = 0;
            }

            var builds = Build.GetAllBuilds();
            var target = Array.Find(builds, m => m.name.Equals(build, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                BuildScript.BuildBundles(new BuildTask(target, buildVersion));
            }
            else
            {
                BuildScript.BuildBundles();
            }
        }

        /// <summary>
        ///     打包安装包
        /// </summary>
        public static void BuildPlayer()
        {
            var config = GetArg("-config");
            var offlineMode = GetArg("-offline");
            Debug.LogFormat("CommandLine.BuildPlayer {0}", config);
            var settings = Settings.GetDefaultSettings();
            var splitConfigs = EditorUtility.FindAssets<SplitConfig>();
            if (!string.IsNullOrEmpty(config))
            {
                foreach (var splitConfig in splitConfigs)
                {
                    if (!splitConfig.name.Equals(config))
                    {
                        settings.splitConfig = splitConfig;
                        continue;
                    }

                    break;
                }
            }
            if (!string.IsNullOrEmpty(offlineMode))
            {
                if (bool.TryParse(offlineMode, out var value) && value)
                {
                    settings.scriptPlayMode = ScriptPlayMode.Preload;
                }
                else
                {
                    settings.scriptPlayMode = ScriptPlayMode.Increment;
                }
            }
            else
            {
                settings.scriptPlayMode = ScriptPlayMode.Preload;
            }
            settings.Save();
            BuildScript.BuildPlayer();
        }
    }
}