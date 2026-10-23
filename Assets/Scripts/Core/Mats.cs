using System.Collections.Generic;
using UnityEngine;

namespace Ravenbrook
{
    /// <summary>Materiais, malhas primitivas e texturas geradas por código.</summary>
    public static class Mats
    {
        static Shader surf, world, decal, glow;
        static Texture2D flatNormal, soft;
        static readonly Dictionary<PrimitiveType, Mesh> prims = new Dictionary<PrimitiveType, Mesh>();
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        static Shader Load(string res, string fallback)
        {
            var s = Resources.Load<Shader>(res);
            if (s == null || !s.isSupported) s = Shader.Find(fallback);
            return s;
        }

        public static Shader SurfShader => surf != null ? surf : (surf = Load("Shaders/Surface", "Standard"));
        static Shader WorldShader => world != null ? world : (world = Load("Shaders/World", "Standard"));
        static Shader DecalShader => decal != null ? decal : (decal = Load("Shaders/Decal", "Sprites/Default"));
        static Shader GlowShader => glow != null ? glow : (glow = Load("Shaders/Glow", "Sprites/Default"));

        public static Texture2D FlatNormal
        {
            get
            {
                if (flatNormal != null) return flatNormal;
                flatNormal = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(128, 128, 255, 255);
                flatNormal.SetPixels32(px); flatNormal.Apply();
                return flatNormal;
            }
        }

        public static Material Lit(Color c, float gloss = 0.15f, float metal = 0f, Texture2D tex = null)
        {
            string key = "L" + c + gloss + metal + (tex ? tex.GetHashCode() : 0);
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(SurfShader) { color = c };
            if (tex) m.mainTexture = tex;
            m.SetFloat("_Glossiness", gloss);
            m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", FlatNormal);
            cache[key] = m;
            return m;
        }

        public static Material Emissive(Color c, Color emission)
        {
            string key = "E" + c + emission;
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(SurfShader) { color = c };
            m.SetColor("_EmissionColor", emission);
            if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", FlatNormal);
            m.EnableKeyword("_EMISSION");
            cache[key] = m;
            return m;
        }

        public static Material World(Color top, Color bottom, float split, Texture2D tex, float scale, float gloss, float dirt)
        {
            var m = new Material(WorldShader) { color = top };
            m.mainTexture = tex;
            if (m.HasProperty("_Color2"))
            {
                m.SetColor("_Color2", bottom); m.SetFloat("_Split", split);
                m.SetFloat("_Scale", scale); m.SetFloat("_Dirt", dirt);
            }
            m.SetFloat("_Glossiness", gloss);
            return m;
        }

        public static Material Decal(Texture2D tex, Color c)
        {
            string key = "D" + tex.GetHashCode() + c;
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(DecalShader) { mainTexture = tex, color = c };
            cache[key] = m;
            return m;
        }

        public static Material Glow(Color c, Texture2D tex = null)
        {
            string key = "G" + c + (tex ? tex.GetHashCode() : 0);
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(GlowShader) { color = c, mainTexture = tex ? tex : Soft };
            cache[key] = m;
            return m;
        }

        public static Mesh Prim(PrimitiveType t)
        {
            if (!prims.TryGetValue(t, out var m) || m == null)
            {
                var g = GameObject.CreatePrimitive(t);
                m = g.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(g);
                prims[t] = m;
            }
            return m;
        }

        /// <summary>Peça visual sem colisor.</summary>
        public static GameObject Part(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Material mat, string name = "peca", Vector3? euler = null)
        {
            var g = new GameObject(name);
            g.layer = parent ? parent.gameObject.layer : 0;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (euler.HasValue) g.transform.localRotation = Quaternion.Euler(euler.Value);
            g.AddComponent<MeshFilter>().sharedMesh = Prim(t);
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        // ---------------- texturas ----------------

        static Texture2D NewTex(int size, bool mips = true)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, mips)
            { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        }

        /// <summary>Ruído que se repete sem emenda (u, v entre 0 e 1).</summary>
        public static float TileNoise(float u, float v, float scale, float ox, float oy, int octaves = 4)
        {
            float a = Fbm(u * scale + ox, v * scale + oy, octaves);
            float b = Fbm((u - 1) * scale + ox, v * scale + oy, octaves);
            float c = Fbm(u * scale + ox, (v - 1) * scale + oy, octaves);
            float d = Fbm((u - 1) * scale + ox, (v - 1) * scale + oy, octaves);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        static float Fbm(float x, float y, int oct)
        {
            float amp = 0.5f, f = 1f, s = 0f, norm = 0f;
            for (int i = 0; i < oct; i++) { s += amp * Mathf.PerlinNoise(x * f, y * f); norm += amp; amp *= 0.5f; f *= 2f; }
            return s / norm;
        }

        public static Texture2D FloorTiles()
        {
            if (texCache.TryGetValue("floor", out var t)) return t;
            const int S = 256; t = NewTex(S);
            var px = new Color[S * S];
            Color a = new Color(0.62f, 0.58f, 0.5f), b = new Color(0.2f, 0.24f, 0.22f), grout = new Color(0.12f, 0.12f, 0.11f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    int tx = x / 64, ty = y / 64;
                    bool g = x % 64 < 2 || y % 64 < 2;
                    Color c = ((tx + ty) & 1) == 0 ? a : b;
                    float n = TileNoise(u, v, 6f, 3.1f, 7.7f);
                    float stain = Mathf.SmoothStep(0.55f, 0.75f, TileNoise(u, v, 2.5f, 11f, 2f));
                    c *= 0.8f + n * 0.4f;
                    c = Color.Lerp(c, new Color(0.18f, 0.14f, 0.1f), stain * 0.5f);
                    if (g) c = grout;
                    c.a = g ? 0.1f : 0.9f - stain * 0.5f;
                    px[y * S + x] = c;
                }
            t.SetPixels(px); t.Apply();
            return texCache["floor"] = t;
        }

        public static Texture2D Plaster()
        {
            if (texCache.TryGetValue("plaster", out var t)) return t;
            const int S = 256; t = NewTex(S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float n = TileNoise(u, v, 8f, 1.3f, 4.2f);
                    float streak = Mathf.SmoothStep(0.6f, 0.85f, TileNoise(u, v * 0.25f, 5f, 9f, 1f));
                    float crack = Mathf.Abs(TileNoise(u, v, 4f, 20f, 20f, 3) - 0.5f) < 0.008f ? 0.6f : 1f;
                    float l = (0.82f + n * 0.25f) * (1 - streak * 0.35f) * crack;
                    px[y * S + x] = new Color(l, l * 0.97f, l * 0.9f, 0.25f);
                }
            t.SetPixels(px); t.Apply();
            return texCache["plaster"] = t;
        }

        public static Texture2D Concrete()
        {
            if (texCache.TryGetValue("concrete", out var t)) return t;
            const int S = 128; t = NewTex(S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = TileNoise(x / (float)S, y / (float)S, 10f, 5f, 5f);
                    float l = 0.55f + n * 0.4f;
                    px[y * S + x] = new Color(l, l, l * 0.96f, 0.15f);
                }
            t.SetPixels(px); t.Apply();
            return texCache["concrete"] = t;
        }

        public static Texture2D Wood()
        {
            if (texCache.TryGetValue("wood", out var t)) return t;
            const int S = 128; t = NewTex(S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float ring = Mathf.Sin((u * 18f + TileNoise(u, v, 3f, 2f, 9f) * 6f) * Mathf.PI) * 0.5f + 0.5f;
                    float plank = (x % 32 < 1) ? 0.5f : 1f;
                    float l = (0.6f + ring * 0.25f) * plank;
                    px[y * S + x] = new Color(0.45f * l, 0.3f * l, 0.18f * l, 0.3f);
                }
            t.SetPixels(px); t.Apply();
            return texCache["wood"] = t;
        }

        public static Texture2D Soft
        {
            get
            {
                if (soft != null) return soft;
                const int S = 64; soft = NewTex(S, false);
                soft.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(S / 2f - 0.5f, S / 2f - 0.5f)) / (S / 2f);
                        float a = Mathf.Clamp01(1 - d); a *= a;
                        px[y * S + x] = new Color(1, 1, 1, a);
                    }
                soft.SetPixels(px); soft.Apply();
                return soft;
            }
        }

        /// <summary>Mancha de sangue com respingos.</summary>
        public static Texture2D Splat(int seed)
        {
            string key = "splat" + seed;
            if (texCache.TryGetValue(key, out var t)) return t;
            const int S = 128; t = NewTex(S, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var rng = new System.Random(seed);
            var px = new Color[S * S];
            var drops = new List<Vector3>();
            for (int i = 0; i < 14; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2, dist = 0.25f + (float)rng.NextDouble() * 0.6f;
                drops.Add(new Vector3(0.5f + Mathf.Cos(ang) * dist * 0.5f, 0.5f + Mathf.Sin(ang) * dist * 0.5f, 0.015f + (float)rng.NextDouble() * 0.04f));
            }
            float ox = (float)rng.NextDouble() * 50f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
                    float edge = 0.28f + (Mathf.PerlinNoise(u * 6 + ox, v * 6) - 0.5f) * 0.22f;
                    float a = Mathf.SmoothStep(edge, edge - 0.04f, d);
                    foreach (var dr in drops)
                        a = Mathf.Max(a, Mathf.SmoothStep(dr.z, dr.z * 0.6f, Vector2.Distance(new Vector2(u, v), new Vector2(dr.x, dr.y))));
                    float dark = 0.5f + Mathf.PerlinNoise(u * 12 + ox, v * 12) * 0.5f;
                    px[y * S + x] = new Color(0.32f * dark, 0.02f, 0.02f, a * 0.92f);
                }
            t.SetPixels(px); t.Apply();
            return texCache[key] = t;
        }

        /// <summary>Marca de bala na parede.</summary>
        public static Texture2D BulletHole()
        {
            if (texCache.TryGetValue("hole", out var t)) return t;
            const int S = 32; t = NewTex(S, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16f;
                    float a = d < 0.25f ? 1f : Mathf.Clamp01(1 - (d - 0.25f) * 1.6f) * 0.6f;
                    px[y * S + x] = new Color(0.04f, 0.035f, 0.03f, a);
                }
            t.SetPixels(px); t.Apply();
            return texCache["hole"] = t;
        }

        /// <summary>"Cookie" da lanterna: anel mais forte e borda irregular.</summary>
        public static Texture2D FlashlightCookie()
        {
            if (texCache.TryGetValue("cookie", out var t)) return t;
            const int S = 128;
            t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(S / 2f, S / 2f)) / (S / 2f);
                    float core = Mathf.Clamp01(1 - d * 1.05f);
                    float ring = Mathf.Exp(-Mathf.Pow((d - 0.55f) * 9f, 2)) * 0.25f;
                    float a = Mathf.Clamp01(core * 0.9f + ring + 0.1f) * Mathf.SmoothStep(1f, 0.85f, d);
                    px[y * S + x] = new Color(a, a, a, a);
                }
            t.SetPixels(px); t.Apply();
            return texCache["cookie"] = t;
        }
    }
}
