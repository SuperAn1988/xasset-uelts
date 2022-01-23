using UnityEngine;

namespace xasset
{
    internal class LocalBundle : Bundle
    {
        private AssetBundleCreateRequest _request;

        protected override void OnLoad()
        {
            _request = LoadAssetBundleAsync(pathOrURL);
        }

        public override void LoadImmediate()
        {
            if (isDone)
            {
                return;
            }

            OnLoaded(_request.assetBundle);
            _request = null;
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading)
            {
                return;
            }

            progress = _request.progress;
            if (_request.isDone)
            {
                OnLoaded(_request.assetBundle);
            }
        }
    }
}