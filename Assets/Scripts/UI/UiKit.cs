using UnityEngine;
using UnityEngine.UI;

namespace ShardsOfTime
{
    // Small uGUI helpers (Hostile Horizon's UiKit) plus a few shapes drawn in code (heart, circle, diamond, arrow) for the HUD.
    public static class UiKit
    {
        static Font font;
        public static Font Font => font ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static readonly Color Accent = new Color(1f, .78f, .3f), Dim = new Color(.8f, .84f, .9f), Ink = new Color(.97f, .98f, 1f), Panel = new Color(.08f, .1f, .18f, .82f);

        public static Canvas ScreenCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = 1;
            return c;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.offsetMin = offMin; rt.offsetMax = offMax; return rt;
        }

        // a box of size w x h with its pivot at an anchor point
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        public static Image Image(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax, Sprite sprite = null)
        {
            var img = Rect(parent, name, anchorMin, anchorMax, offMin, offMax).gameObject.AddComponent<Image>(); img.color = color; img.raycastTarget = false; img.sprite = sprite; return img;
        }

        public static Text Label(Transform parent, string name, int size, TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var t = Rect(parent, name, anchorMin, anchorMax, offMin, offMax).gameObject.AddComponent<Text>();
            t.font = Font; t.fontSize = size; t.alignment = align; t.color = color; t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, .75f); sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        // ---- shapes drawn into textures (no art files needed)
        static Sprite heart, circle, diamond, arrow, ring;
        public static Sprite Heart => heart ? heart : heart = Shape("heart", 64, (x, y) => { x *= 1.25f; y = y * 1.25f + .25f; float a = x * x + y * y - 1; return a * a * a - x * x * y * y * y <= 0; });
        public static Sprite Circle => circle ? circle : circle = Shape("circle", 64, (x, y) => x * x + y * y <= 1);
        public static Sprite Ring => ring ? ring : ring = Shape("ring", 64, (x, y) => { float r = x * x + y * y; return r <= 1 && r >= .72f; });
        public static Sprite Diamond => diamond ? diamond : diamond = Shape("diamond", 64, (x, y) => Mathf.Abs(x) * 1.6f + Mathf.Abs(y) <= 1);
        public static Sprite Arrow => arrow ? arrow : arrow = Shape("arrow", 64, (x, y) => y > -.2f && Mathf.Abs(x) < (1 - y) * .6f || y <= -.2f && y > -1 && Mathf.Abs(x) < .22f);

        static Sprite Shape(string name, int n, System.Func<float, float, bool> inside)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int hits = 0;   // 4x supersampling for soft edges
                    for (int s = 0; s < 4; s++) { float x = ((i + .25f + .5f * (s % 2)) / n) * 2 - 1, y = ((j + .25f + .5f * (s / 2)) / n) * 2 - 1; if (inside(x, y)) hits++; }
                    px[j * n + i] = new Color32(255, 255, 255, (byte)(hits * 63.75f));
                }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
        }
    }
}
