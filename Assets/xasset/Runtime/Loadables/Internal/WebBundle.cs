using UnityEngine;
using UnityEngine.Networking;

namespace xasset
{
    public class WebBundle : Bundle
    {
        private AsyncOperation _operation;
        private UnityWebRequest _request;

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                customLoader += CustomLoader;
            }
        }

        public override void LoadImmediate()
        {
            if (isDone)
            {
                return;
            }

            // 这个地方比较玄学，之前在 win 10 系统有这样成功过，不过 win 11 上跪了，官方文档不允许这样： https://docs.unity3d.com/cn/current/Manual/webgl-networking.html
            while (!_request.isDone)
            {
            }
            
            OnLoaded(DownloadHandlerAssetBundle.GetContent(_request));
            FinishRequest();
        }

        private static Bundle CustomLoader(string url, ManifestBundle info)
        {
            return new WebBundle { info = info, pathOrURL = url };
        }

        protected override void OnLoad()
        {
            _request = UnityWebRequestAssetBundle.GetAssetBundle(pathOrURL);
            _operation = _request.SendWebRequest();
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading)
            {
                return;
            }

            if (_request == null || _operation == null)
            {
                return;
            }

            progress = _operation.progress;
            if (!string.IsNullOrEmpty(_request.error))
            {
                Finish(_request.error);
                FinishRequest();
                return;
            }

            if (!_operation.isDone)
            {
                return;
            }
            OnLoaded(DownloadHandlerAssetBundle.GetContent(_request));
            FinishRequest();
        }

        private void FinishRequest()
        {
            _request.Dispose();
            _request = null;
            _operation = null;
        }
    }
}