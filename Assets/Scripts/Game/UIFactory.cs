using UnityEngine;
using UnityEngine.UI;

namespace SevenPages.Game
{
    /// <summary>用代码搭 uGUI 的小工具，避免手工编辑场景文件。</summary>
    public static class UIFactory
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>铺满父级的纯色面板。</summary>
        public static Image CreatePanel(string name, Transform parent, Color color, float alpha = 1f)
        {
            RectTransform rt = CreateRect(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = rt.gameObject.AddComponent<Image>();
            Color c = color;
            c.a = alpha;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor)
        {
            RectTransform rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Theme.GetFont();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>带背景与文字的可点击按钮。</summary>
        public static Button CreateButton(string name, Transform parent, string label, int fontSize, Color bg, Color textColor)
        {
            RectTransform rt = CreateRect(name, parent);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            Text text = CreateText("Label", rt, label, fontSize, textColor, TextAnchor.MiddleCenter);
            RectTransform labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(6, 4);
            labelRt.offsetMax = new Vector2(-6, -4);

            return btn;
        }

        /// <summary>把 rect 锚定到父级的某个矩形区域（按 0~1 比例）。</summary>
        public static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY, float padding = 0f)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>创建一个按比例锚定的区域，color.a 为 0 时不留底板。</summary>
        public static RectTransform CreateRegion(string name, Transform parent, float minX, float minY, float maxX, float maxY, Color color)
        {
            RectTransform rt = CreateRect(name, parent);
            SetAnchors(rt, minX, minY, maxX, maxY);
            if (color.a > 0f)
            {
                var img = rt.gameObject.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }
            return rt;
        }
    }
}
