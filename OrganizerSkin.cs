using UnityEngine;

namespace SFSBlueprintOrganizer
{

    internal static class OrganizerSkin
    {
        public static readonly Color Ink = new Color(0.93f, 0.94f, 0.96f, 1f);
        public static readonly Color InkDim = new Color(0.78f, 0.82f, 0.87f, 0.9f);
        public static readonly Color InkFaint = new Color(0.62f, 0.67f, 0.74f, 0.7f);

        private static readonly Color PanelTop = new Color32(57, 82, 117, 235);
        private static readonly Color PanelBottom = new Color32(39, 64, 99, 235);

        private static readonly Color ButtonTop = new Color32(50, 68, 93, 255);
        private static readonly Color ButtonBottom = new Color32(24, 40, 64, 255);

        private static readonly Color SelectedBorder = new Color32(216, 221, 227, 255);

        private static readonly Color FieldColor = new Color32(20, 33, 52, 255);

        public static GUIStyle Panel;
        public static GUIStyle Title;
        public static GUIStyle Header;
        public static GUIStyle Label;
        public static GUIStyle Hint;
        public static GUIStyle TextField;
        public static GUIStyle Chip;
        public static GUIStyle ChipSelected;
        public static GUIStyle Button;

        private static bool _init;
        private static Font _nativeFont;

        public static void EnsureInit(Font nativeFont = null)
        {
            if (_init && nativeFont == _nativeFont) return;
            _init = true;
            _nativeFont = nativeFont;

            Texture2D panelTex = MakeRoundedRect(56, 13, PanelTop, PanelBottom, null, 0);
            Texture2D buttonTex = MakeRoundedRect(26, 7, ButtonTop, ButtonBottom, null, 0);
            Texture2D buttonSelectedTex = MakeRoundedRect(26, 7, ButtonTop, ButtonBottom, SelectedBorder, 2f);
            Texture2D fieldTex = MakeRoundedRect(20, 6, FieldColor, FieldColor, null, 0);

            Panel = new GUIStyle
            {
                normal = { background = panelTex },
                border = new RectOffset(17, 17, 17, 17),
                padding = new RectOffset(10, 10, 8, 8)
            };

            Title = new GUIStyle(GUI.skin.label)
            {
                font = nativeFont,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Ink }
            };

            Header = new GUIStyle(GUI.skin.label)
            {
                font = nativeFont,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };

            Label = new GUIStyle(GUI.skin.label)
            {
                font = nativeFont,
                fontSize = 11,
                normal = { textColor = InkDim }
            };

            Hint = new GUIStyle(GUI.skin.label)
            {
                font = nativeFont,
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = InkFaint }
            };

            TextField = new GUIStyle(GUI.skin.textField)
            {
                font = nativeFont,
                fontSize = 12,
                normal = { textColor = Ink, background = fieldTex },
                focused = { textColor = Ink, background = fieldTex },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(6, 6, 4, 4)
            };

            Chip = new GUIStyle
            {
                font = nativeFont,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = InkDim, background = buttonTex },
                hover = { textColor = Ink, background = buttonTex },
                border = new RectOffset(10, 10, 10, 10),
                padding = new RectOffset(8, 8, 3, 3),
                margin = new RectOffset(2, 2, 2, 2)
            };

            ChipSelected = new GUIStyle(Chip)
            {
                normal = { textColor = Ink, background = buttonSelectedTex },
                hover = { textColor = Ink, background = buttonSelectedTex }
            };

            Button = new GUIStyle(Chip)
            {
                fontSize = 12
            };
        }

        private static Texture2D MakeRoundedRect(int size, float radius, Color topColor, Color bottomColor, Color? borderColor, float borderWidth)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float half = size / 2f;
            float innerHalf = half - radius;

            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                Color fill = Color.Lerp(bottomColor, topColor, t);

                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float qx = Mathf.Abs(px - half) - innerHalf;
                    float qy = Mathf.Abs(py - half) - innerHalf;
                    float dist = Mathf.Min(Mathf.Max(qx, qy), 0f)
                                 + new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                                 - radius;

                    Color pixel;
                    if (dist > 0.75f)
                    {
                        pixel = new Color(0, 0, 0, 0);
                    }
                    else
                    {
                        float edgeAlpha = Mathf.Clamp01(0.75f - dist);
                        if (borderColor.HasValue && dist > -borderWidth)
                            pixel = new Color(borderColor.Value.r, borderColor.Value.g, borderColor.Value.b, edgeAlpha);
                        else
                            pixel = new Color(fill.r, fill.g, fill.b, fill.a * edgeAlpha);
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Font TryGetNativeFont(SFS.UI.LoadMenu menu)
        {
            if (menu == null || menu.title == null) return null;

            try
            {
                object unityText = SafeAccess.GetRaw(menu.title, "UnityText");
                if (unityText != null)
                {
                    var font = SafeAccess.Get<Font>(unityText, "font");
                    if (font != null) return font;
                }

                object tmpText = SafeAccess.GetRaw(menu.title, "TMProText");
                if (tmpText != null)
                {
                    object fontAsset = SafeAccess.GetRaw(tmpText, "font");
                    if (fontAsset != null)
                    {
                        var sourceFont = SafeAccess.Get<Font>(fontAsset, "sourceFontFile");
                        if (sourceFont != null) return sourceFont;
                    }
                }
            }
            catch
            {

            }

            return null;
        }
    }
}
