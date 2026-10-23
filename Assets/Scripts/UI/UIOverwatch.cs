using UnityEngine;
using UnityEngine.UI;

namespace Ravenbrook
{
    /// <summary>
    /// Painel Overwatch (F1): ferramentas de teste do próprio jogo, divididas nos módulos
    /// Guardian (integridade), Ordnance (recursos) e Phantom (reconhecimento).
    /// Usar qualquer opção marca a partida como "de teste" (sem classificação).
    /// </summary>
    public partial class UIRoot
    {
        RectTransform overwatch;
        Button owGod, owTele, owAmmo, owKnife, owFreeze;
        Slider owTime; Text owTimeLabel;

        static string Check(bool on, string label) => (on ? "[■]  " : "[  ]  ") + label;

        RectTransform BuildOverwatch()
        {
            var p = UIKit.Box(root, "Overwatch", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-520, 30), new Vector2(-30, -30));
            UIKit.Img(p, new Color(0.02f, 0.03f, 0.025f, 0.93f));
            var border = UIKit.Box(p, "borda", new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(3, 0));
            UIKit.Img(border, UIKit.Accent);
            UIKit.Header(p, Loc.Get("ow_title"), 60);
            var body = UIKit.Box(p, "corpo", Vector2.zero, Vector2.one, new Vector2(22, 20), new Vector2(-22, -76));
            UIKit.VStack(body, 7);

            void Section(string key)
            {
                var g = new GameObject("secao", typeof(RectTransform)); g.transform.SetParent(body, false);
                g.AddComponent<LayoutElement>().preferredHeight = 40;
                var t = UIKit.Label(g.transform, Loc.Get(key), 22, UIKit.Warn, TextAnchor.LowerLeft);
                t.fontStyle = FontStyle.Bold;
            }
            Button Toggle(string key, System.Func<bool> get, System.Action<bool> set)
            {
                Button b = null;
                b = UIKit.Btn(body, Check(get(), Loc.Get(key)), () =>
                {
                    set(!get()); Gm.MarkCheat();
                    UIKit.SetBtnText(b, Check(get(), Loc.Get(key)));
                }, 22, 46);
                return b;
            }

            Section("ow_guardian");
            owGod = Toggle("ow_god", () => Cheats.God, v => Cheats.God = v);
            UIKit.Btn(body, "✚  " + Loc.Get("ow_heal"), () => { if (Gm.Player) { Gm.Player.Heal(999); Gm.MarkCheat(); } }, 22, 46);
            owTele = Toggle("ow_telemetry", () => Cheats.Telemetry, v => Cheats.Telemetry = v);

            Section("ow_ordnance");
            owAmmo = Toggle("ow_ammo", () => Cheats.InfiniteAmmo, v => Cheats.InfiniteAmmo = v);
            owKnife = Toggle("ow_knife", () => Cheats.UnbreakableKnife, v => Cheats.UnbreakableKnife = v);
            UIKit.Btn(body, "◆  " + Loc.Get("ow_arsenal"), GiveArsenal, 22, 46);
            UIKit.Btn(body, "↻  " + Loc.Get("ow_cycle"), () => { if (Gm.Player) Gm.Player.Weapons.Cycle(); }, 22, 46);

            Section("ow_phantom");
            owFreeze = Toggle("ow_freeze", () => Cheats.FreezeEnemies, v => Cheats.FreezeEnemies = v);
            UIKit.Btn(body, "⌂  " + Loc.Get("ow_reset"), () => { foreach (var z in Zombie.All) z.ReturnHome(); Gm.MarkCheat(); }, 22, 46);
            var tl = new GameObject("tempo", typeof(RectTransform)); tl.transform.SetParent(body, false);
            tl.AddComponent<LayoutElement>().preferredHeight = 30;
            owTimeLabel = UIKit.Label(tl.transform, Loc.F("ow_time", Gm.GameSpeed), 22, UIKit.TextC, TextAnchor.LowerLeft);
            owTime = UIKit.Sld(body, 0.1f, 2f, Gm.GameSpeed, v =>
            {
                Gm.GameSpeed = Mathf.Round(v * 20f) / 20f;
                owTimeLabel.text = Loc.F("ow_time", Gm.GameSpeed);
                Gm.ApplyTime();
                if (Mathf.Abs(Gm.GameSpeed - 1f) > 0.01f) Gm.MarkCheat();
            });

            var tp = new GameObject("tp", typeof(RectTransform)); tp.transform.SetParent(body, false);
            tp.AddComponent<LayoutElement>().preferredHeight = 32;
            UIKit.Label(tp.transform, Loc.Get("ow_teleport"), 22, UIKit.TextC, TextAnchor.LowerLeft);
            var gridGo = new GameObject("salas", typeof(RectTransform)); gridGo.transform.SetParent(body, false);
            gridGo.AddComponent<LayoutElement>().preferredHeight = 3 * 44 + 2 * 6;
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(146, 44); grid.spacing = new Vector2(6, 6); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            for (int room = 0; room < 9; room++)
            {
                int k = room;
                var b = UIKit.Btn(gridGo.transform, Loc.Get(LevelBuilder.RoomKeys[room]), () => Teleport(k), 17, 44);
                b.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(12, 0);
            }

            var note = new GameObject("nota", typeof(RectTransform)); note.transform.SetParent(body, false);
            note.AddComponent<LayoutElement>().preferredHeight = 50;
            UIKit.Label(note.transform, Loc.Get("ow_note") + "   [F1]", 18, UIKit.Dim, TextAnchor.MiddleLeft);
            return p;
        }

        void RefreshOverwatch()
        {
            UIKit.SetBtnText(owGod, Check(Cheats.God, Loc.Get("ow_god")));
            UIKit.SetBtnText(owTele, Check(Cheats.Telemetry, Loc.Get("ow_telemetry")));
            UIKit.SetBtnText(owAmmo, Check(Cheats.InfiniteAmmo, Loc.Get("ow_ammo")));
            UIKit.SetBtnText(owKnife, Check(Cheats.UnbreakableKnife, Loc.Get("ow_knife")));
            UIKit.SetBtnText(owFreeze, Check(Cheats.FreezeEnemies, Loc.Get("ow_freeze")));
            owTime.SetValueWithoutNotify(Gm.GameSpeed);
            owTimeLabel.text = Loc.F("ow_time", Gm.GameSpeed);
        }

        void GiveArsenal()
        {
            var p = Gm.Player; if (p == null) return;
            for (int i = 0; i < 4; i++) { p.Weapons.Owned[i] = true; p.Weapons.Mag[i] = WeaponSystem.Defs[i].Mag; }
            p.Weapons.KnifeDur = 100;
            p.Inv.Add(ItemId.AmmoP, 30); p.Inv.Add(ItemId.AmmoS, 10); p.Inv.Add(ItemId.AmmoM, 6);
            Gm.MarkCheat();
            Toast(Loc.Get("ow_arsenal"));
        }

        public void Teleport(int room)
        {
            var p = Gm.Player; if (p == null) return;
            Vector3 pos = Gm.Level.RoomPoint(room);
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) pos = hit.position;
            p.Teleport(pos);
            Gm.MarkCheat();
        }
    }
}
