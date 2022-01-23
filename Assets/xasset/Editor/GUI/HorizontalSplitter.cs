using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    public class HorizontalSplitter
    {
        public float percent = 0.65f;
        public Rect rect;
        public int size = 3;
        public bool resizing { get; protected set; }


        public void OnGUI(Rect position)
        {
            rect.x = (int)(position.xMin + position.width * percent);
            rect.width = size;
            rect.height = position.height;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown &&
                rect.Contains(Event.current.mousePosition))
            {
                resizing = true;
            }

            if (resizing)
            {
                var mousePosInRect = Event.current.mousePosition.x - position.xMin;
                percent = Mathf.Clamp(mousePosInRect / position.width, 0.60f, 0.80f);
                rect.x = (int)(position.width * percent + position.yMin);

                if (Event.current.type == EventType.MouseUp)
                {
                    resizing = false;
                }
            }
            else
            {
                percent = Mathf.Clamp(percent, 0.60f, 0.80f);
            }
        }
    }
}