using System.Collections.Generic;
using UnityEngine;

namespace Ravenbrook
{
    /// <summary>Pós-processamento da câmera (vinheta, granulado e pulso vermelho).</summary>
    [RequireComponent(typeof(Camera))]
    public class PostFX : MonoBehaviour
    {
        public static PostFX I;
        public float HitFlash;
        Material mat;

        void Awake()
        {
            I = this;
            var sh = Resources.Load<Shader>("Shaders/PostFX");
            if (sh != null && sh.isSupported) mat = new Material(sh);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null || !Settings.PostFX) { Graphics.Blit(src, dst); return; }
            float red = HitFlash, desat = 0.25f;
            var p = GameManager.I != null ? GameManager.I.Player : null;
            if (p != null)
            {
                if (p.Hp <= 33) red += 0.45f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                else if (p.Hp <= 66) red += 0.12f;
                if (p.Dead) desat = 0.9f;
            }
            HitFlash = Mathf.MoveTowards(HitFlash, 0, Time.unscaledDeltaTime * 2f);
            mat.SetFloat("_Vignette", 1.1f);
            mat.SetFloat("_Grain", 0.045f);
            mat.SetFloat("_Desat", desat);
            mat.SetFloat("_Red", red);
            mat.SetVector("_Tint", new Vector4(0.96f, 1.0f, 0.95f, 1));
            Graphics.Blit(src, dst, mat);
        }
    }

    /// <summary>
    /// Modelos 3D opcionais em Resources/Modelos (gerados no Blender ou baixados do Sketchfab).
    /// Se o arquivo não existir, o jogo usa peças primitivas.
    /// </summary>
    public static class ModelLibrary
    {
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        public static GameObject Get(string name)
        {
            if (Application.isBatchMode && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-semModelos") >= 0) return null;
            if (!cache.TryGetValue(name, out var g))
            {
                g = Resources.Load<GameObject>("Modelos/" + name);
                cache[name] = g;
            }
            return g;
        }

        /// <summary>Instancia o modelo e o ajusta para caber em <paramref name="size"/> (maior dimensão), apoiado no chão.</summary>
        public static GameObject Spawn(string name, Transform parent, Vector3 localPos, float size, float yaw = 0f)
        {
            var prefab = Get(name);
            if (prefab == null) return null;
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = localPos;
            holder.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var inst = Object.Instantiate(prefab, holder.transform, false);
            StripColliders(inst);
            if (size > 0) FitTo(inst.transform, holder.transform, size);
            return holder;
        }

        public static void StripColliders(GameObject g)
        {
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }

        public static void FitTo(Transform model, Transform holder, float size)
        {
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (max < 0.0001f) return;
            float k = size / max;
            model.localScale *= k;
            b.center = holder.position + (b.center - holder.position) * k;
            b.extents *= k;
            Vector3 delta = holder.position - new Vector3(b.center.x, b.min.y, b.center.z);
            model.position += delta;
        }

        public static string Credits()
        {
            var t = Resources.Load<TextAsset>("Modelos/CREDITOS");
            return t != null ? t.text : "";
        }
    }

    /// <summary>Efeitos: sangue, faíscas, marcas, rastros de bala.</summary>
    public static class Fx
    {
        static ParticleSystem blood, spark;
        static readonly Queue<GameObject> decals = new Queue<GameObject>();
        static Transform root;

        static Transform Root
        {
            get
            {
                if (root == null) root = new GameObject("Efeitos").transform;
                return root;
            }
        }

        static ParticleSystem MakeSystem(string name, Material mat, float gravity, float life)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.maxParticles = 600;
            main.startLifetime = life; main.startSize = 0.05f; main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            ps.Play();
            return ps;
        }

        public static void Blood(Vector3 p, Vector3 dir, int count = 14)
        {
            if (blood == null)
            {
                var m = new Material(Shader.Find("Sprites/Default")) { mainTexture = Mats.Soft };
                blood = MakeSystem("Sangue", m, 1.6f, 0.7f);
            }
            for (int i = 0; i < count; i++)
            {
                var ep = new ParticleSystem.EmitParams
                {
                    position = p,
                    velocity = (dir * Random.Range(0.5f, 2.5f) + Random.insideUnitSphere * 1.4f),
                    startColor = new Color(Random.Range(0.25f, 0.45f), 0.01f, 0.01f, 0.95f),
                    startSize = Random.Range(0.03f, 0.09f),
                    startLifetime = Random.Range(0.4f, 0.9f)
                };
                blood.Emit(ep, 1);
            }
        }

        public static void Spark(Vector3 p, Vector3 n)
        {
            if (spark == null) spark = MakeSystem("Faiscas", Mats.Glow(new Color(1f, 0.75f, 0.4f, 1f)), 0.8f, 0.25f);
            for (int i = 0; i < 8; i++)
            {
                var ep = new ParticleSystem.EmitParams
                {
                    position = p + n * 0.02f,
                    velocity = (n + Random.insideUnitSphere * 0.8f) * Random.Range(1.5f, 4f),
                    startColor = new Color(1f, 0.8f, 0.45f, 1f),
                    startSize = Random.Range(0.02f, 0.05f),
                    startLifetime = Random.Range(0.1f, 0.3f)
                };
                spark.Emit(ep, 1);
            }
            var dust = new ParticleSystem.EmitParams { position = p + n * 0.05f, velocity = n * 0.3f, startColor = new Color(0.6f, 0.58f, 0.52f, 0.4f), startSize = 0.25f, startLifetime = 0.6f };
            spark.Emit(dust, 1);
        }

        /// <summary>Quad com textura alinhado à superfície. Mantém no máximo 80 no mapa.</summary>
        public static GameObject Decal(Vector3 p, Vector3 n, float size, Texture2D tex, Transform parent = null, float alpha = 1f)
        {
            var g = new GameObject("marca");
            g.transform.SetParent(parent != null ? parent : Root, true);
            g.transform.position = p + n * 0.012f;
            g.transform.rotation = Quaternion.LookRotation(-n, Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            g.transform.localScale = Vector3.one * size;
            g.AddComponent<MeshFilter>().sharedMesh = Mats.Prim(PrimitiveType.Quad);
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mats.Decal(tex, new Color(1, 1, 1, alpha));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (parent == null)
            {
                decals.Enqueue(g);
                while (decals.Count > 80) { var old = decals.Dequeue(); if (old) Object.Destroy(old); }
            }
            return g;
        }

        public static void Tracer(Vector3 a, Vector3 b)
        {
            var g = new GameObject("rastro");
            g.transform.SetParent(Root);
            var lr = g.AddComponent<LineRenderer>();
            lr.positionCount = 2; lr.SetPosition(0, a); lr.SetPosition(1, b);
            lr.startWidth = 0.025f; lr.endWidth = 0.008f;
            lr.sharedMaterial = Mats.Glow(new Color(1f, 0.85f, 0.55f, 0.5f));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(g, 0.05f);
        }

        public static void ClearDecals()
        {
            while (decals.Count > 0) { var d = decals.Dequeue(); if (d) Object.Destroy(d); }
        }
    }
}
