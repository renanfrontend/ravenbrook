using System.Collections.Generic;
using UnityEngine;

namespace Ravenbrook
{
    /// <summary>
    /// Todos os sons são sintetizados na inicialização (tiros, gemidos, coração, portas, ambiente).
    /// Nenhum arquivo de áudio externo.
    /// </summary>
    public static class Sfx
    {
        const int SR = 22050;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioSource ui, ambient, heart, tensionSrc;
        static readonly List<AudioSource> pool = new List<AudioSource>();
        static int poolIdx;
        static System.Random rng = new System.Random(7);

        public static float Tension;   // 0..1 (inimigos perseguindo)
        public static float Heart;     // 0..1 (saúde baixa)

        public static void Init(GameObject host)
        {
            ui = host.AddComponent<AudioSource>(); ui.playOnAwake = false;
            ambient = host.AddComponent<AudioSource>(); ambient.loop = true; ambient.volume = 0.4f;
            heart = host.AddComponent<AudioSource>(); heart.loop = true; heart.volume = 0f;
            tensionSrc = host.AddComponent<AudioSource>(); tensionSrc.loop = true; tensionSrc.volume = 0f;
            Generate();
            ambient.clip = clips["drone"]; ambient.Play();
            heart.clip = clips["heart"]; heart.Play();
            tensionSrc.clip = clips["pulse"]; tensionSrc.Play();
            var poolRoot = new GameObject("SonsNoMundo").transform;
            poolRoot.SetParent(host.transform);
            for (int i = 0; i < 20; i++)
            {
                var s = new GameObject("som").AddComponent<AudioSource>();
                s.transform.SetParent(poolRoot);
                s.playOnAwake = false; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 1.5f; s.maxDistance = 30f; s.dopplerLevel = 0f;
                pool.Add(s);
            }
            host.AddComponent<SfxRunner>();
        }

        public static void Play(string name, float vol = 1f)
        {
            if (clips.TryGetValue(name, out var c)) ui.PlayOneShot(c, vol);
        }

        public static void PlayAt(string name, Vector3 pos, float vol = 1f, float pitch = 1f, float maxDist = 30f)
        {
            if (!clips.TryGetValue(name, out var c)) return;
            var s = pool[poolIdx]; poolIdx = (poolIdx + 1) % pool.Count;
            s.transform.position = pos; s.clip = c; s.volume = vol; s.pitch = pitch; s.maxDistance = maxDist;
            s.Play();
        }

        public static void UpdateMix(float dt)
        {
            heart.volume = Mathf.MoveTowards(heart.volume, Heart * 0.9f, dt);
            heart.pitch = 1f + Heart * 0.7f;
            tensionSrc.volume = Mathf.MoveTowards(tensionSrc.volume, Tension * 0.45f, dt * 0.5f);
            ambient.pitch = 1f - Tension * 0.06f;
        }

        // ---------------- síntese ----------------

        static float N() => (float)(rng.NextDouble() * 2 - 1);
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static float[] Buf(float dur) => new float[(int)(dur * SR)];

        static void Add(string name, float[] d, float peak = 0.9f)
        {
            float max = 0.0001f;
            foreach (var v in d) max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < d.Length; i++) d[i] = d[i] / max * peak;
            var clip = AudioClip.Create(name, d.Length, 1, SR, false);
            clip.SetData(d, 0);
            clips[name] = clip;
        }

        /// <summary>Filtro de estado variável (passa-baixa e passa-banda).</summary>
        struct Svf
        {
            float low, band;
            public float Low(float x, float fc, float q = 0.7f) { Step(x, fc, q); return low; }
            public float Band(float x, float fc, float q = 0.4f) { Step(x, fc, q); return band; }
            void Step(float x, float fc, float q)
            {
                float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(fc, SR * 0.2f) / SR);
                low += f * band;
                float high = x - low - q * band;
                band += f * high;
            }
        }

        static float[] Shot(float dur, float decay, float cutoff, float boomHz, float boomAmt)
        {
            var d = Buf(dur); var f = new Svf();
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * decay);
                float noise = f.Low(N(), cutoff * (0.3f + env * 0.7f));
                float boom = Mathf.Sin(2 * Mathf.PI * boomHz * t * (1 - t * 0.6f)) * Mathf.Exp(-t * decay * 0.5f) * boomAmt;
                float crack = t < 0.005f ? N() : 0f;
                d[i] = noise * env * 1.6f + boom + crack * 0.7f;
            }
            return d;
        }

        static float[] Groan(float dur, float f0, float fall)
        {
            var d = Buf(dur); var f1 = new Svf(); var f2 = new Svf(); var fb = new Svf();
            float ph = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SR, k = t / dur;
                float vib = 1 + 0.05f * Mathf.Sin(2 * Mathf.PI * 5.5f * t) + 0.04f * Mathf.Sin(2 * Mathf.PI * 1.3f * t);
                float freq = f0 * vib * (1 - fall * k);
                ph += freq / SR;
                float saw = 2 * (ph % 1f) - 1;
                float voice = f1.Band(saw, 520f, 0.3f) * 1.2f + f2.Band(saw, 1150f, 0.35f) * 0.6f;
                float breath = fb.Band(N(), 900f, 0.8f) * 0.35f;
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * k), 0.6f) * (0.75f + 0.25f * Mathf.Sin(2 * Mathf.PI * 3.1f * t));
                d[i] = (voice + breath) * env;
            }
            return d;
        }

        static float Thump(float t, float t0, float hz, float decay)
        {
            if (t < t0) return 0;
            float x = t - t0;
            return Mathf.Sin(2 * Mathf.PI * hz * x) * Mathf.Exp(-x * decay);
        }

        static float[] Clicks(float dur, float[] times, float fc)
        {
            var d = Buf(dur); var f = new Svf();
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SR, v = 0;
                foreach (var tc in times) if (t >= tc && t < tc + 0.03f) v += N() * Mathf.Exp(-(t - tc) * 160);
                d[i] = f.Band(v, fc, 0.25f) * 2f + v * 0.3f;
            }
            return d;
        }

        static void Generate()
        {
            Add("pistol", Shot(0.35f, 22f, 5200f, 140f, 0.6f));
            Add("shotgun", Shot(0.7f, 9f, 3000f, 85f, 0.9f));
            Add("magnum", Shot(0.9f, 7f, 3800f, 60f, 1.1f));
            Add("empty", Clicks(0.08f, new[] { 0f }, 3500f), 0.5f);
            Add("reload", Clicks(0.8f, new[] { 0f, 0.32f, 0.62f }, 2800f), 0.6f);

            { // golpe de faca
                var d = Buf(0.22f); var f = new Svf();
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR, k = t / 0.22f; d[i] = f.Band(N(), 700 + 2600 * k, 0.5f) * Mathf.Sin(Mathf.PI * k); }
                Add("knife", d, 0.6f);
            }
            { // passo
                var d = Buf(0.14f); var f = new Svf();
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR; d[i] = f.Low(N(), 700f) * Mathf.Exp(-t * 40) + Thump(t, 0, 85, 50) * 0.5f; }
                Add("step", d, 0.5f);
            }
            Add("groan1", Groan(1.6f, 78f, 0.1f));
            Add("groan2", Groan(1.9f, 64f, 0.2f));
            Add("groan3", Groan(1.3f, 92f, -0.1f));
            Add("zdie", Groan(2.2f, 82f, 0.55f));
            Add("hurt", Groan(0.35f, 150f, 0.3f), 0.7f);
            { // impacto na carne
                var d = Buf(0.25f); var f = new Svf();
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR; d[i] = f.Low(N(), 1400f) * Mathf.Exp(-t * 28) + Thump(t, 0, 160, 30) * 0.7f; }
                Add("zhit", d, 0.7f);
            }
            { // mordida
                var d = Buf(0.45f); var f = new Svf();
                float[] ts = { 0f, 0.09f, 0.19f };
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SR, v = 0;
                    foreach (var tc in ts) if (t >= tc) v += N() * Mathf.Exp(-(t - tc) * 35);
                    d[i] = f.Band(v, 1300f, 0.5f) * 1.5f;
                }
                Add("bite", d, 0.8f);
            }
            { // porta rangendo
                var d = Buf(1.15f); var f = new Svf(); float ph = 0;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SR, k = t / 1.15f;
                    float fr = 210 + 90 * Mathf.Sin(k * 7f) + 40 * Mathf.PerlinNoise(t * 30f, 0);
                    ph += fr / SR;
                    float saw = 2 * (ph % 1f) - 1;
                    d[i] = f.Band(saw * (0.6f + 0.4f * Mathf.PerlinNoise(t * 60f, 3)), 900f, 0.2f) * Mathf.Sin(Mathf.PI * Mathf.Min(1, k * 1.2f)) * 0.8f
                           + Thump(t, 1.0f, 70, 25) * 0.9f;
                }
                Add("door", d, 0.7f);
            }
            Add("locked", Clicks(0.4f, new[] { 0f, 0.08f, 0.17f, 0.24f }, 1800f), 0.6f);
            { // item coletado
                var d = Buf(0.5f);
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR; d[i] = Thump(t, 0, 880, 12) * 0.6f + Thump(t, 0.11f, 1320, 8) * 0.6f; }
                Add("pickup", d, 0.5f);
            }
            { // clique de interface
                var d = Buf(0.09f);
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR; d[i] = Thump(t, 0, 660, 60) + Thump(t, 0.02f, 990, 70) * 0.5f; }
                Add("ui", d, 0.35f);
            }
            { // coração (60 bpm no tom normal)
                var d = Buf(1.0f);
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR; d[i] = Thump(t, 0, 48, 22) + Thump(t, 0.28f, 44, 24) * 0.7f; }
                Add("heart", d, 0.9f);
            }
            { // pulso de tensão (loop de 2 s)
                var d = Buf(2.0f);
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SR, bt = t % 0.5f;
                    d[i] = Mathf.Sin(2 * Mathf.PI * 55 * t) * Mathf.Exp(-bt * 6) * 0.7f + Mathf.Sin(2 * Mathf.PI * 110.5f * t) * 0.15f * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 0.5f * t));
                }
                Add("pulse", d, 0.8f);
            }
            { // pisada do perseguidor
                var d = Buf(0.6f); var f = new Svf();
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR; d[i] = Thump(t, 0, 38, 8) + f.Low(N(), 400f) * Mathf.Exp(-t * 22) * 0.8f; }
                Add("stomp", d, 0.95f);
            }
            { // rugido
                var d = Buf(2.0f); var f = new Svf(); var fn = new Svf(); float ph = 0;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SR, k = t / 2f;
                    ph += (58 + 8 * Mathf.Sin(t * 13)) / SR;
                    float saw = 2 * (ph % 1f) - 1;
                    d[i] = (f.Band(saw, 320f, 0.4f) * 1.4f + fn.Band(N(), 600f, 0.6f) * 0.6f) * Mathf.Pow(Mathf.Sin(Mathf.PI * k), 0.4f);
                }
                Add("roar", d, 0.95f);
            }
            { // susto (acorde dissonante)
                var d = Buf(2.4f); float[] fr = { 196f, 207.6f, 293.7f, 311.1f };
                var ph = new float[4];
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SR, v = 0;
                    for (int j = 0; j < 4; j++) { ph[j] += fr[j] * (1 + 0.004f * Mathf.Sin(t * 9 + j)) / SR; v += 2 * (ph[j] % 1f) - 1; }
                    float env = Mathf.Min(1, t * 40) * Mathf.Exp(-t * 1.1f) * (0.8f + 0.2f * Mathf.Sin(2 * Mathf.PI * 9 * t));
                    d[i] = v * 0.25f * env + (t < 0.15f ? N() * (0.15f - t) * 3 : 0);
                }
                Add("sting", d, 0.8f);
            }
            { // máquina de escrever
                var times = new List<float>(); float tt = 0;
                for (int i = 0; i < 8; i++) { times.Add(tt); tt += R(0.07f, 0.16f); }
                var d = Clicks(tt + 0.9f, times.ToArray(), 2400f);
                for (int i = 0; i < d.Length; i++) { float t = i / (float)SR; d[i] += Thump(t, tt + 0.15f, 1760, 5) * 0.5f; }
                Add("save", d, 0.6f);
            }
            { // portão de metal
                var d = Buf(2.6f); var f = new Svf(); var fb = new Svf();
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SR, k = t / 2.6f;
                    float rattle = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * 18 * t);
                    d[i] = f.Low(N(), 500f) * rattle * Mathf.Sin(Mathf.PI * k) + fb.Band(N(), 2200 + 600 * k, 0.15f) * 0.25f * Mathf.Sin(Mathf.PI * k) + Thump(t, 2.4f, 50, 15);
                }
                Add("gate", d, 0.8f);
            }
            { // ambiente: drone grave que se repete sem emenda (10 s)
                const float D = 10f; var d = Buf(D); var f = new Svf();
                float[] fr = { 55f, 55.4f, 82.5f, 110.7f, 164.8f };
                float[] amp = { 0.5f, 0.4f, 0.25f, 0.15f, 0.06f };
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SR, v = 0;
                    for (int j = 0; j < fr.Length; j++) v += Mathf.Sin(2 * Mathf.PI * fr[j] * t) * amp[j] * (0.7f + 0.3f * Mathf.Sin(2 * Mathf.PI * 0.1f * t * (j + 1)));
                    float wind = f.Low(N(), 300f + 200f * Mathf.Sin(2 * Mathf.PI * 0.2f * t)) * 0.5f;
                    float edge = Mathf.Min(1, Mathf.Min(t, D - t) * 4f);
                    d[i] = v + wind * edge;
                }
                Add("drone", d, 0.7f);
            }
        }
    }

    public class SfxRunner : MonoBehaviour
    {
        void Update() => Sfx.UpdateMix(Time.unscaledDeltaTime);
    }
}
