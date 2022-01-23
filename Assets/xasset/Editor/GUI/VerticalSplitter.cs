using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    public class VerticalSplitter
    {
        public float percent = 0.8f;
        public Rect rect;
        public int size = 3;
        public bool resizing { get; protected set; }

        public void OnGUI(Rect position)
        {
            rect.y = (int)(position.yMin + position.height * percent);
            rect.width = position.width;
            rect.height = size;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown &&
                rect.Contains(Event.current.mousePosition))
            {
                resizing = true;
            }

            if (resizing)
            {
                var mousePosInRect = Event.current.mousePosition.y - position.yMin;
                percent = Mathf.Clamp(mousePosInRect / position.height, 0.20f, 0.90f);
                rect.y = (int)(position.height * percent + position.yMin);

                if (Event.current.type == EventType.MouseUp)
                {
                    resizing = false;
                }
            }
            else
            {
                percent = Mathf.Clamp(percent, 0.20f, 0.90f);
            }
        }
    }
}