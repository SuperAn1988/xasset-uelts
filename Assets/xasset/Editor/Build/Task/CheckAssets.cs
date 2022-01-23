using System.Collections.Generic;
using UnityEngine;

namespace xasset.editor
{
    public class CheckAssets : BuildTaskJob
    {
        public CheckAssets(BuildTask task) : base(task)
        {
        }

        public override void Run()
        {
            var pathWithAssets = new Dictionary<string, GroupAsset>();
            var bundledAssets = _task.bundledAssets;
            for (var i = 0; i < bundledAssets.Count; i++)
            {
                var asset = bundledAssets[i];
                if (!pathWithAssets.TryGetValue(asset.path, out var ba))
                {
                    pathWithAssets[asset.path] = asset;
                }
                else
                {
                    bundledAssets.RemoveAt(i);
                    i--;
                    Debug.LogWarningFormat("{0} can't pack with {1}, because already pack to {2}", asset.path,
                        asset.bundle, ba.bundle);
                }
            }
        }
    }
}