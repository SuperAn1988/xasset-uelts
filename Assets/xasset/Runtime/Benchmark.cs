using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace xasset
{
    public class Benchmark : MonoBehaviour
    {
        public InputField inputField;
        public Dropdown dropdown;
        public GameObject loadItemTemplate;
        private readonly List<string> assets = new List<string>();
        private readonly List<LoadItem> loadingItems = new List<LoadItem>();
        private readonly List<LoadItem> loadItems = new List<LoadItem>();

        private IEnumerator Start()
        {
            if (!Versions.Initialized)
            {
                yield return Versions.InitializeAsync();
            }
            dropdown.options.Clear();
            foreach (var manifest in Versions.Manifests)
            {
                dropdown.options.Add(new Dropdown.OptionData(manifest.name));
            }
            dropdown.value = -1;
            dropdown.value = 0;
            dropdown.onValueChanged.AddListener(OnValueChanged);
            inputField.onValueChanged.AddListener(OnValueChanged);
            loadItems.Add(new LoadItem(loadItemTemplate));
            ApplySelection(0);
        }

        private void Update()
        {
            if (loadingItems.Count > 0)
            {
                for (var index = 0; index < loadingItems.Count; index++)
                {
                    var asset = loadingItems[index];
                    if (!asset.loadable.isDone)
                    {
                        asset.time.text = $"Loading {0}%{asset.loadable.progress * 100}";
                    }
                    else
                    {
                        asset.time.text = $"{asset.loadable.elapsed}/{asset.loadable.frames}";
                        loadingItems.RemoveAt(index);
                        index--;
                    }
                }
            }
        }

        public void LoadAll()
        {
            for (var index = 0; index < assets.Count; index++)
            {
                LoadAsset(loadItems[index]);
            }
        }

        public void UnloadAll()
        {
            foreach (var item in loadItems)
            {
                var loadItem = item;
                if (loadItem.loadable != null)
                {
                    loadItem.loadable.Release();
                    loadItem.loadable = null;
                }
                loadItem.time.text = "Click to load.";
            }
        }

        public void LoadAsset(LoadItem item)
        {
            if (item.loadable != null)
            {
                return;
            }
            var path = item.name.text;
            if (Versions.IsRawAsset(path))
            {
                item.loadable = RawAsset.LoadAsync(path);
            }
            else if (path.EndsWith(".unity"))
            {
                item.loadable = Scene.LoadAsync(path, null, true);
            }
            else
            {
                item.loadable = Asset.LoadAsync(path, typeof(Object));
            }
            loadingItems.Add(item);
        }

        public void ReloadAssets()
        {
            var expands = assets.Count - loadItems.Count;
            if (expands > 0)
            {
                for (var i = 0; i < expands; i++)
                {
                    var go = Instantiate(loadItemTemplate, loadItemTemplate.transform.parent);
                    loadItems.Add(new LoadItem(go));
                }
            }
            for (var index = 0; index < loadItems.Count; index++)
            {
                var item = loadItems[index];
                if (index < assets.Count)
                {
                    var asset = assets[index];
                    item.name.text = asset;
                    item.time.text = "Click to load.";
                    item.button.onClick.RemoveAllListeners();
                    item.button.onClick.AddListener(() =>
                    {
                        LoadAsset(item);
                    });
                    item.go.SetActive(true);
                }
                else
                {
                    item.go.SetActive(false);
                }
            }
        }

        private void OnValueChanged(string arg0)
        {
            ApplySelection(dropdown.value, s => s.Contains(arg0));
        }

        private void OnValueChanged(int arg0)
        {
            ApplySelection(arg0);
        }

        private void ApplySelection(int selected, Func<string, bool> filter = null)
        {
            assets.Clear();
            if (selected >= 0 && selected <= Versions.Manifests.Count)
            {
                var manifest = Versions.Manifests[selected];
                if (filter != null)
                {
                    foreach (var asset in manifest.assets)
                    {
                        if (filter(asset.path))
                        {
                            assets.Add(asset.path);
                        }
                    }
                }
                else
                {
                    foreach (var asset in manifest.assets)
                    {
                        assets.Add(asset.path);
                    }
                }
            }
            ReloadAssets();
        }

        public class LoadItem
        {
            public readonly Button button;
            public readonly GameObject go;
            public readonly Text name;
            public readonly Text time;
            public Loadable loadable;

            public LoadItem(GameObject o)
            {
                go = o;
                button = go.GetComponent<Button>();
                time = go.transform.Find("Time").GetComponent<Text>();
                name = go.GetComponent<Text>();
                loadable = null;
            }
        }
    }
}