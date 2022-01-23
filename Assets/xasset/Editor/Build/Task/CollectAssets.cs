using System;
using System.Collections.Generic;

namespace xasset.editor
{
    public class CollectAssets : BuildTaskJob
    {
        public static Action<CollectAssets> onCollectAssets;
        public readonly List<Group> _groups = new List<Group>();
        private readonly List<GroupAsset> bundledAssets = new List<GroupAsset>();
        private readonly List<GroupAsset> rawAssets = new List<GroupAsset>();

        public CollectAssets(BuildTask task, IEnumerable<Group> groups) : base(task)
        {
            _groups.AddRange(groups);
        }

        public void AddBundledAsset(GroupAsset asset)
        {
            bundledAssets.Add(asset);
        }

        public void AddRawAsset(GroupAsset asset)
        {
            rawAssets.Add(asset);
        }

        public override void Run()
        {
            // 采集资源
            for (var i = 0; i < _groups.Count; i++)
            {
                var group = _groups[i];
                group.build = _task.name;
                DisplayProgressBar("采集资源", group.name, i, _groups.Count);
                Group.CollectAssets(group, (path, entry) =>
                {
                    var asset = group.CreateAsset(path, entry);
                    asset.bundle = Group.PackAsset(asset);
                    if (group.bundleMode == BundleMode.PackByRaw)
                        rawAssets.Add(asset);
                    else
                        bundledAssets.Add(asset);
                });
            }

            onCollectAssets?.Invoke(this);
            _task.bundledAssets.AddRange(bundledAssets);
            _task.rawAssets.AddRange(rawAssets);
            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }
}