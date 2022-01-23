using System.IO;
using UnityEngine;

namespace xasset.example
{
    public class LoadRawAsset : MonoBehaviour
    {
        public void Load()
        {
            var asset = RawAsset.Load("versions");
            asset.Release();
            var text = File.ReadAllText(asset.savePath);
            Debug.Log(text);
        }
    }
}