using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ravenbrook
{
    /// <summary>Tema HAZMAT (escuro, verde-amarelado de risco biológico) e construtores de Unity UI.</summary>
    public static class UIKit
    {
        public static Font Font, Serif;
        public static readonly Color Bg = new Color(0.025f, 0.035f, 0.03f, 0.94f);
        public static readonly Color PanelC = new Color(0.06f, 0.08f, 0.065f, 0.96f);
        public static readonly Color Accent = new Color(0.74f, 0.85f, 0.22f);
        public static readonly Color Warn = new Color(0.93f, 0.7f, 0.12f);
        public static readonly Color Blood = new Color(0.68f, 0.06f, 0.05f);
        public static readonly Color TextC = new Color(0.86f, 0.89f, 0.83f);
        public static readonly Color Dim = new Color(0.5f, 0.56f, 0.48f);
        public static readonly Color Line = new Color(0.74f, 0.85f, 0.22f, 0.35f);

        static Sprite white, stripes, gradient, arrow;
        static readonly Dictionary<ItemId, Sprite> icons = new Dictionary<ItemId, Sprite>();

        public static void Init()
        {
            if (Font != null) return;
            Font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Yu Gothic UI", "Malgun Gothic", "Microsoft YaHei UI", "Microsoft YaHei", "Meiryo", "Arial Unicode MS", "Arial" }, 32);
            Serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Yu Mincho", "Batang", "SimSun", "Segoe UI" }, 64);
        }

        // ---------------- sprites ----------------

        static Sprite Make(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);

        public static Sprite White
        {
            get
            {
                if (white == null) { var t = new Texture2D(4, 4); var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white; t.SetPixels(px); t.Apply(); white = Make(t); }
                return white;
            }
        }

        public static Sprite Stripes
        {
            get
            {
                if (stripes != null) return stripes;
                var t = new Texture2D(64, 16) { wrapMode = TextureWrapMode.Repeat };
                for (int y = 0; y < 16; y++) for (int x = 0; x < 64; x++)
                        t.SetPixel(x, y, ((x + y) / 8) % 2 == 0 ? Warn : new Color(0.05f, 0.05f, 0.04f));
                t.Apply();
                return stripes = Make(t);
            }
        }

        public static Sprite Gradient
        {
            get
            {
                if (gradient != null) return gradient;
                var t = new Texture2D(256, 4) { wrapMode = TextureWrapMode.Clamp };
                for (int x = 0; x < 256; x++) for (int y = 0; y < 4; y++)
                        t.SetPixel(x, y, new Color(0, 0, 0, Mathf.Lerp(0.95f, 0f, Mathf.Pow(x / 255f, 1.4f))));
                t.Apply();
                return gradient = Make(t);
            }
        }

        public static Sprite Arrow
        {
            get
            {
                if (arrow != null) return arrow;
                const int S = 32; var t = new Texture2D(S, S);
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                    {
                        float half = (y / (float)S) * 0.5f;     // ponta para cima
                        bool inside = Mathf.Abs(x / (float)S - 0.5f) < 0.5f - half && y > 2;
                        t.SetPixel(x, y, inside ? Color.white : Color.clear);
                    }
                t.Apply();
                return arrow = Make(t);
            }
        }

        /// <summary>Ícone desenhado por código para cada item.</summary>
        public static Sprite Icon(ItemId id)
        {
            if (icons.TryGetValue(id, out var sp)) return sp;
            const int S = 64;
            var px = new Color[S * S];
            var info = ItemDB.Get(id);
            void Rect(int x0, int y0, int x1, int y1, Color c) { for (int y = Mathf.Max(0, y0); y < Mathf.Min(S, y1); y++) for (int x = Mathf.Max(0, x0); x < Mathf.Min(S, x1); x++) px[y * S + x] = c; }
            void Circle(float cx, float cy, float r, Color c) { for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) px[y * S + x] = c; }
            void Ellipse(float cx, float cy, float rx, float ry, Color c) { for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) { float dx = (x - cx) / rx, dy = (y - cy) / ry; if (dx * dx + dy * dy <= 1) px[y * S + x] = c; } }
            Color dark = new Color(0.1f, 0.08f, 0.06f);
            switch (info.Kind)
            {
                case ItemKind.Heal when id == ItemId.Spray:
                    Rect(22, 8, 42, 50, new Color(0.88f, 0.9f, 0.92f)); Rect(26, 50, 38, 56, new Color(0.4f, 0.4f, 0.42f));
                    Rect(29, 18, 35, 40, new Color(0.2f, 0.7f, 0.3f)); Rect(21, 26, 43, 32, new Color(0.2f, 0.7f, 0.3f));
                    break;
                case ItemKind.Heal:
                    Rect(18, 6, 46, 22, new Color(0.5f, 0.3f, 0.18f)); Rect(16, 20, 48, 24, new Color(0.4f, 0.24f, 0.14f));
                    Color a = id == ItemId.HerbR ? ItemDB.Get(ItemId.HerbR).Color : ItemDB.Get(ItemId.HerbG).Color;
                    Color b = id == ItemId.HerbM ? ItemDB.Get(ItemId.HerbR).Color : a;
                    Ellipse(22, 36, 9, 13, a); Ellipse(42, 36, 9, 13, b); Ellipse(32, 44, 9, 14, a); Ellipse(32, 30, 6, 9, b);
                    break;
                case ItemKind.Ammo:
                    Rect(10, 8, 54, 34, info.Color * 0.8f); Rect(14, 14, 50, 26, new Color(0.9f, 0.88f, 0.8f));
                    int n = id == ItemId.AmmoS ? 4 : 5;
                    for (int i = 0; i < n; i++)
                    {
                        int x0 = 13 + i * (40 / n);
                        Color shell = id == ItemId.AmmoS ? new Color(0.75f, 0.15f, 0.12f) : new Color(0.85f, 0.65f, 0.3f);
                        Rect(x0, 34, x0 + 6, id == ItemId.AmmoM ? 54 : 50, shell);
                        Rect(x0, 34, x0 + 6, 38, new Color(0.7f, 0.55f, 0.25f));
                    }
                    break;
                case ItemKind.Key:
                    Circle(18, 44, 11, info.Color); Circle(18, 44, 5, Color.clear);
                    Rect(26, 41, 56, 47, info.Color); Rect(46, 32, 50, 41, info.Color); Rect(52, 34, 56, 41, info.Color);
                    break;
                case ItemKind.Medal:
                    Circle(32, 32, 24, info.Color * 0.75f); Circle(32, 32, 20, info.Color);
                    if (id == ItemId.MedalSun) { Circle(32, 32, 8, dark); for (int k = 0; k < 8; k++) { float ang = k * Mathf.PI / 4; Circle(32 + Mathf.Cos(ang) * 14, 32 + Mathf.Sin(ang) * 14, 2.5f, dark); } }
                    else if (id == ItemId.MedalMoon) { Circle(30, 32, 12, dark); Circle(36, 35, 11, info.Color); }
                    else for (int k = 0; k < 5; k++) { float ang = Mathf.PI / 2 + k * Mathf.PI * 2 / 5; Circle(32 + Mathf.Cos(ang) * 10, 32 + Mathf.Sin(ang) * 10, 4f, dark); Circle(32, 32, 5, dark); }
                    break;
            }
            var t = new Texture2D(S, S) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(px); t.Apply();
            return icons[id] = Make(t);
        }

        // ---------------- construtores ----------------

        public static RectTransform Box(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
            return r;
        }

        public static RectTransform Fill(Transform parent, string name, float pad = 0) =>
            Box(parent, name, Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));

        /// <summary>Retângulo de tamanho fixo ancorado num ponto (pivô = âncora).</summary>
        public static RectTransform At(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var r = Box(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            r.pivot = anchor; r.sizeDelta = size; r.anchoredPosition = pos;
            return r;
        }

        public static Image Img(RectTransform r, Color c, Sprite s = null)
        {
            var i = r.gameObject.AddComponent<Image>();
            i.sprite = s ?? White; i.color = c;
            if (s == Stripes) { i.type = Image.Type.Tiled; }
            return i;
        }

        public static Text Label(Transform parent, string text, int size, Color c, TextAnchor a, bool serif = false, string name = "texto")
        {
            var r = Fill(parent, name);
            var t = r.gameObject.AddComponent<Text>();
            t.font = serif ? Serif : Font; t.fontSize = size; t.color = c; t.alignment = a; t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static void Shadow(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.9f); s.effectDistance = new Vector2(2, -2);
        }

        public static Button Btn(Transform parent, string label, Action onClick, int size = 28, float height = 58)
        {
            var go = new GameObject("botao_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.sizeDelta = new Vector2(380, height);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = height; le.minHeight = height;
            var img = Img(r, Color.white);
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = new Color(0.09f, 0.11f, 0.09f, 0.92f);
            cb.highlightedColor = new Color(0.26f, 0.32f, 0.08f, 1f);
            cb.pressedColor = new Color(0.5f, 0.6f, 0.12f, 1f);
            cb.selectedColor = cb.normalColor;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            var bar = Box(r, "barra", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 6), new Vector2(5, -6));
            Img(bar, Accent).raycastTarget = false;
            var t = Label(r, label, size, TextC, TextAnchor.MiddleLeft);
            t.rectTransform.offsetMin = new Vector2(22, 0);
            b.onClick.AddListener(() => { Sfx.Play("ui"); onClick?.Invoke(); });
            return b;
        }

        public static void SetBtnText(Button b, string s) => b.GetComponentInChildren<Text>().text = s;

        public static VerticalLayoutGroup VStack(RectTransform r, float spacing, int pad = 0, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            v.childAlignment = align;
            return v;
        }

        public static Slider Sld(Transform parent, float min, float max, float value, Action<float> onChange)
        {
            var go = new GameObject("slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 34;
            var bg = Box(r, "fundo", new Vector2(0, 0.35f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
            Img(bg, new Color(0.12f, 0.15f, 0.12f));
            var fillArea = Box(r, "area", new Vector2(0, 0.35f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
            var fill = Box(fillArea, "preenchido", Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            Img(fill, Accent);
            var handleArea = Box(r, "alca_area", Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            var handle = Box(handleArea, "alca", new Vector2(0, 0), new Vector2(0, 1), new Vector2(-9, 0), new Vector2(9, 0));
            var hi = Img(handle, TextC);
            var s = go.AddComponent<Slider>();
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = hi;
            s.minValue = min; s.maxValue = max; s.value = value;
            s.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return s;
        }

        /// <summary>Faixa de risco (amarelo/preto) com título.</summary>
        public static void Header(RectTransform parent, string title, float height = 54)
        {
            var h = Box(parent, "cabecalho", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -height), Vector2.zero);
            Img(h, new Color(0.04f, 0.05f, 0.04f, 1f));
            var st = Box(h, "listras", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 8));
            Img(st, Color.white, Stripes);
            var t = Label(h, title, 26, Accent, TextAnchor.MiddleLeft);
            t.rectTransform.offsetMin = new Vector2(20, 8); t.fontStyle = FontStyle.Bold;
        }
    }
}
