using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    public static class EditorUtility
    {
        /// <summary>
        ///     自定义版本记录的上下文菜单。
        /// </summary>
        public static Action<GenericMenu, BuildRecord> customContextMenu;

        public static void PingWithSelected(Object target)
        {
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        public static T[] FindAssets<T>() where T : ScriptableObject
        {
            var builds = new List<T>();
            var guilds = AssetDatabase.FindAssets("t:" + typeof(T).FullName);
            foreach (var guild in guilds)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guild);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                builds.Add(asset);
            }

            return builds.ToArray();
        }

        public static T FindOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var guilds = AssetDatabase.FindAssets($"t:{typeof(T).FullName}");
            foreach (var guild in guilds)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guild);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                var asset = GetOrCreateAsset<T>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                return asset;
            }

            return GetOrCreateAsset<T>(path);
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            Utility.CreateDirectoryIfNecessary(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void SaveAsset(Object asset)
        {
            UnityEditor.EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        public static void AddExportMenuItem(GenericMenu menu, SplitConfig config, params string[] paths)
        {
            menu.AddItem(new GUIContent("Export to/" + config.name), false, data =>
            {
                if (data is SplitConfig split)
                {
                    var objects = new HashSet<Object>(split.assets);
                    foreach (var item in paths)
                    {
                        objects.Add(AssetDatabase.LoadAssetAtPath(item, typeof(Object)));
                    }

                    split.assets = objects.ToArray();
                    SaveAsset(split);
                }
            }, config);
        }
    }
}