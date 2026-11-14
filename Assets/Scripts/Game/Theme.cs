using UnityEngine;

namespace SevenPages.Game
{
    /// <summary>全局配色与字体（暗色书页风）。占位美术到位后这里只改颜色。</summary>
    public static class Theme
    {
        public static readonly Color Background = new Color(0.06f, 0.06f, 0.08f, 1f);
        public static readonly Color Panel = new Color(0.12f, 0.11f, 0.15f, 1f);
        public static readonly Color PanelLight = new Color(0.17f, 0.16f, 0.21f, 1f);
        public static readonly Color Ink = new Color(0.88f, 0.88f, 0.92f, 1f);
        public static readonly Color InkDim = new Color(0.55f, 0.54f, 0.60f, 1f);
        public static readonly Color Accent = new Color(0.78f, 0.22f, 0.28f, 1f);
        public static readonly Color Spirit = new Color(0.36f, 0.58f, 0.92f, 1f);
        public static readonly Color Page = new Color(0.72f, 0.66f, 0.52f, 1f);
        public static readonly Color Curse = new Color(0.45f, 0.25f, 0.50f, 1f);

        public static Font GetFont()
        {
            // Unity 2022+ 内置字体名
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
