using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace xasset.example.editor
{
    public static class Tools
    {
        [MenuItem("Tools/ReplaceFont")]
        public static void ReplaceFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/xasset/Example/Arts/Fonts/Helvetica.ttc");
            var texts = Object.FindObjectsOfType<Text>();
            foreach (var text in texts)
            {
                text.font = font;
                Debug.Log($"replace {text.name} to {font.name}");
            }
        }
    }
}