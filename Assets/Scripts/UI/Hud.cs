using UnityEngine;
using UnityEngine.UI;

namespace Ravenbrook
{
    /// <summary>HUD: eletrocardiograma (estado de saúde), arma e munição, mira, aviso de interação, objetivo e telemetria.</summary>
    public class Hud : MonoBehaviour
    {
        const int EW = 240, EH = 64;
        RawImage ecg; Texture2D ecgTex; Color32[] px; float[] hist = new float[EW]; int head; float beat, tick;
        Text status, weapon, ammo, reserve, prompt, objective, telemetry, hint, reload;
        RectTransform crossRoot, objectiveBox, hintBox, knifeRoot; RectTransform[] bars = new RectTransform[4];
        Image hitMark, knifeFill;
        float objectiveT, hintT, fpsAcc; int fpsFrames; float fps;

        public static Hud Create(RectTransform parent)
        {
            var r = UIKit.Fill(parent, "HUD");
            var h = r.gameObject.AddComponent<Hud>();
            h.Build(r);
            return h;
        }

        void Build(RectTransform r)
        {
            // eletrocardiograma no canto inferior esquerdo
            var ep = UIKit.At(r, "ecg", new Vector2(0, 0), new Vector2(36, 36), new Vector2(300, 118));
            UIKit.Img(ep, new Color(0, 0, 0, 0.55f));
            var eb = UIKit.Box(ep, "grafico", new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -38));
            ecg = eb.gameObject.AddComponent<RawImage>();
            ecgTex = new Texture2D(EW, EH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            px = new Color32[EW * EH];
            ecg.texture = ecgTex; ecg.raycastTarget = false;
            var sb = UIKit.Box(ep, "estado", new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -36), new Vector2(-10, -4));
            status = UIKit.Label(sb, "", 24, Color.green, TextAnchor.MiddleLeft);
            status.fontStyle = FontStyle.Bold;

            // arma no canto inferior direito
            var wp = UIKit.At(r, "arma", new Vector2(1, 0), new Vector2(-36, 36), new Vector2(330, 130));
            UIKit.Img(wp, new Color(0, 0, 0, 0.55f));
            var wn = UIKit.Box(wp, "nome", new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -40), new Vector2(-16, -6));
            weapon = UIKit.Label(wn, "", 24, UIKit.Accent, TextAnchor.MiddleRight);
            var wa = UIKit.Box(wp, "municao", new Vector2(0, 0), new Vector2(0.62f, 1), new Vector2(16, 8), new Vector2(0, -38));
            ammo = UIKit.Label(wa, "", 64, UIKit.TextC, TextAnchor.MiddleRight, true);
            var wr = UIKit.Box(wp, "reserva", new Vector2(0.62f, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-16, -38));
            reserve = UIKit.Label(wr, "", 30, UIKit.Dim, TextAnchor.MiddleLeft);
            knifeRoot = UIKit.Box(wp, "faca", new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 14), new Vector2(-16, 22));
            UIKit.Img(knifeRoot, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            var kf = UIKit.Box(knifeRoot, "barra", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            knifeFill = UIKit.Img(kf, UIKit.Accent);
            knifeFill.type = Image.Type.Filled; knifeFill.fillMethod = Image.FillMethod.Horizontal;

            // mira
            crossRoot = UIKit.At(r, "mira", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            var dot = UIKit.At(crossRoot, "ponto", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 4));
            UIKit.Img(dot, new Color(1, 1, 1, 0.9f)).raycastTarget = false;
            for (int i = 0; i < 4; i++)
            {
                bool vert = i < 2;
                bars[i] = UIKit.At(crossRoot, "barra", new Vector2(0.5f, 0.5f), Vector2.zero, vert ? new Vector2(3, 14) : new Vector2(14, 3));
                UIKit.Img(bars[i], new Color(1, 1, 1, 0.85f)).raycastTarget = false;
            }
            var hm = UIKit.At(crossRoot, "acerto", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
            hm.localRotation = Quaternion.Euler(0, 0, 45);
            hitMark = UIKit.Img(hm, new Color(0.9f, 0.15f, 0.1f, 0));
            hitMark.raycastTarget = false;
            var rl = UIKit.At(crossRoot, "recarga", new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(400, 30));
            reload = UIKit.Label(rl, "", 22, UIKit.Warn, TextAnchor.MiddleCenter);

            // aviso de interação
            var pb = UIKit.At(r, "interacao", new Vector2(0.5f, 0), new Vector2(0, 340), new Vector2(900, 50));
            prompt = UIKit.Label(pb, "", 30, UIKit.TextC, TextAnchor.MiddleCenter);
            UIKit.Shadow(prompt);

            // objetivo
            objectiveBox = UIKit.At(r, "objetivo", new Vector2(0, 1), new Vector2(36, -30), new Vector2(900, 80));
            var ot = UIKit.Box(objectiveBox, "t", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            objective = UIKit.Label(ot, "", 26, UIKit.TextC, TextAnchor.UpperLeft);
            UIKit.Shadow(objective);

            // telemetria (Painel Overwatch)
            var tb = UIKit.At(r, "telemetria", new Vector2(1, 1), new Vector2(-30, -30), new Vector2(420, 200));
            telemetry = UIKit.Label(tb, "", 20, UIKit.Accent, TextAnchor.UpperRight);
            UIKit.Shadow(telemetry);

            // dica de controles
            hintBox = UIKit.At(r, "dica", new Vector2(0.5f, 0), new Vector2(0, 175), new Vector2(1300, 70));
            hint = UIKit.Label(hintBox, Loc.Get("controls"), 21, UIKit.Dim, TextAnchor.MiddleCenter);
            UIKit.Shadow(hint);
        }

        public void ShowObjective() { objectiveT = 7f; }
        public void ShowHint() { hintT = 14f; }

        static float Wave(float ph)
        {
            if (ph > 0.1f && ph < 0.18f) return 0.12f * Mathf.Sin((ph - 0.1f) / 0.08f * Mathf.PI);
            if (ph > 0.22f && ph < 0.24f) return -0.15f;
            if (ph >= 0.24f && ph < 0.27f) return 1f - Mathf.Abs(ph - 0.255f) / 0.015f * 0.6f;
            if (ph >= 0.27f && ph < 0.30f) return -0.35f;
            if (ph > 0.42f && ph < 0.56f) return 0.25f * Mathf.Sin((ph - 0.42f) / 0.14f * Mathf.PI);
            return 0f;
        }

        void Update()
        {
            var gm = GameManager.I;
            var p = gm != null ? gm.Player : null;
            if (p == null) return;
            float udt = Time.unscaledDeltaTime;

            // eletrocardiograma
            float bpm = p.Dead ? 0 : p.Hp > 66 ? 72 : p.Hp > 33 ? 98 : 135;
            float amp = p.Hp > 33 ? 1f : 0.75f;
            tick += udt;
            while (tick > 1f / 50f)
            {
                tick -= 1f / 50f;
                beat = (beat + bpm / 60f / 50f) % 1f;
                hist[head] = p.Dead ? 0 : Wave(beat) * amp + Random.Range(-0.02f, 0.02f);
                head = (head + 1) % EW;
            }
            DrawEcg(p.StatusColor);
            status.text = "♥ " + Loc.Get(p.StatusKey);
            status.color = p.StatusColor;

            // arma e munição
            var ws = p.Weapons;
            var def = ws.Def;
            weapon.text = Loc.Get(def.Key).ToUpper();
            bool knife = def.Ammo == ItemId.None;
            ammo.text = knife ? "—" : ws.Mag[ws.Current].ToString();
            ammo.color = !knife && ws.Mag[ws.Current] == 0 ? UIKit.Blood : UIKit.TextC;
            reserve.text = knife ? "" : "/ " + (Cheats.InfiniteAmmo ? "∞" : ws.Reserve(ws.Current).ToString());
            knifeRoot.gameObject.SetActive(knife);
            knifeFill.fillAmount = ws.KnifeDur / 100f;
            reload.text = ws.Reloading ? Loc.Get("reloading") : "";

            // mira com dispersão
            bool showCross = p.Aiming && !knife;
            crossRoot.gameObject.SetActive(showCross || ws.HitMarker > 0);
            if (showCross)
            {
                float fov = gm.Cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float off = Mathf.Tan(ws.SpreadDeg * Mathf.Deg2Rad) / Mathf.Tan(fov) * 540f + 6f;
                bars[0].anchoredPosition = new Vector2(0, off + 7); bars[1].anchoredPosition = new Vector2(0, -off - 7);
                bars[2].anchoredPosition = new Vector2(off + 7, 0); bars[3].anchoredPosition = new Vector2(-off - 7, 0);
            }
            foreach (var b in bars) b.gameObject.SetActive(showCross);
            hitMark.color = new Color(0.9f, 0.15f, 0.1f, ws.HitMarker * 0.8f);

            prompt.text = p.Focus != null ? "[E]  " + p.Focus.Prompt : "";

            objectiveT -= udt; hintT -= udt;
            bool showObj = objectiveT > 0;
            objectiveBox.gameObject.SetActive(showObj);
            if (showObj) objective.text = Loc.Get("objective").ToUpper() + "\n" + gm.LastObjective;
            hintBox.gameObject.SetActive(hintT > 0);

            // telemetria
            fpsAcc += udt; fpsFrames++;
            if (fpsAcc > 0.5f) { fps = fpsFrames / fpsAcc; fpsAcc = 0; fpsFrames = 0; }
            if (Cheats.Telemetry)
            {
                int alive = 0; foreach (var z in Zombie.All) if (!z.Dead) alive++;
                var pos = p.transform.position;
                int room = gm.Level.RoomAt(pos);
                telemetry.text = $"OVERWATCH · TELEMETRIA\nFPS {fps:0}\nHP {p.Hp:0}/{p.MaxHp:0}  {(Cheats.God ? "[INVULNERÁVEL]" : "")}\n" +
                                 $"POS {pos.x:0.0} {pos.z:0.0}  {(room >= 0 ? Loc.Get(LevelBuilder.RoomKeys[room]) : "-")}\n" +
                                 $"INIMIGOS {alive}  PERSEGUINDO {Zombie.Chasing}\nTEMPO {GameManager.FormatTime(gm.PlayTime)}  ×{gm.GameSpeed:0.00}";
            }
            else telemetry.text = "";
        }

        void DrawEcg(Color c)
        {
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            var grid = new Color32(60, 90, 60, 60);
            for (int x = 0; x < EW; x += 20) for (int y = 0; y < EH; y++) px[y * EW + x] = grid;
            for (int y = 0; y < EH; y += 16) for (int x = 0; x < EW; x++) px[y * EW + x] = grid;
            int prevY = -1;
            for (int x = 0; x < EW; x++)
            {
                float v = hist[(head + x) % EW];
                int y = Mathf.Clamp(Mathf.RoundToInt(EH * 0.4f + v * EH * 0.5f), 1, EH - 2);
                byte a = (byte)(Mathf.Clamp01(x / (float)EW * 1.4f) * 255);
                var col = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), a);
                int y0 = prevY < 0 ? y : Mathf.Min(prevY, y), y1 = prevY < 0 ? y : Mathf.Max(prevY, y);
                for (int yy = y0; yy <= y1; yy++) { px[yy * EW + x] = col; if (x + 1 < EW) px[yy * EW + x + 1] = col; }
                prevY = y;
            }
            ecgTex.SetPixels32(px);
            ecgTex.Apply(false);
        }
    }
}
