using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ravenbrook
{
    /// <summary>
    /// Toda a interface (Unity UI) criada por código: título, pausa, opções, morte, vitória,
    /// créditos, documentos, inventário, baú, mapa, HUD e Painel Overwatch.
    /// Ao trocar de idioma tudo é reconstruído.
    /// </summary>
    public partial class UIRoot : MonoBehaviour
    {
        RectTransform root, title, pause, options, gameover, victory, credits, document, toastArea;
        Hud hud;
        Image hitFlash;
        Text docTitle, docBody, victoryStats, victoryRank, victoryCheats;
        Button continueBtn, goContinueBtn;
        readonly List<(Text text, float until)> toasts = new List<(Text, float)>();
        GameState optionsFrom = GameState.Title;

        GameManager Gm => GameManager.I;

        public static UIRoot Create()
        {
            UIKit.Init();
            var go = new GameObject("Interface");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(es);
            }
            var ui = go.AddComponent<UIRoot>();
            ui.root = (RectTransform)go.transform;
            ui.Build();
            return ui;
        }

        void Build()
        {
            hud = Hud.Create(root);
            title = BuildTitle();
            pause = BuildPause();
            options = BuildOptions();
            gameover = BuildGameOver();
            victory = BuildVictory();
            credits = BuildCredits();
            document = BuildDocument();
            inventory = BuildInventory();
            itembox = BuildItemBox();
            map = BuildMap();
            overwatch = BuildOverwatch();
            toastArea = UIKit.Box(root, "Mensagens", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-700, 150), new Vector2(700, 330));
            var v = UIKit.VStack(toastArea, 6, 0, TextAnchor.LowerCenter);
            v.childControlHeight = false;
            var hf = UIKit.Fill(root, "Dano");
            hitFlash = UIKit.Img(hf, new Color(0.6f, 0, 0, 0));
            hitFlash.raycastTarget = false;
            HideAll();
        }

        public void Rebuild()
        {
            bool optionsOpen = options != null && options.gameObject.activeSelf;
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            toasts.Clear();
            Build();
            OnState(Gm.State);
            if (optionsOpen) { HideMenus(); options.gameObject.SetActive(true); }
        }

        void HideMenus()
        {
            foreach (var r in new[] { title, pause, options, gameover, victory, credits, document, inventory, itembox, map })
                if (r != null) r.gameObject.SetActive(false);
        }

        void HideAll()
        {
            HideMenus();
            overwatch.gameObject.SetActive(false);
            hud.gameObject.SetActive(false);
        }

        public void OnState(GameState s)
        {
            HideMenus();
            hud.gameObject.SetActive(s == GameState.Playing);
            overwatch.gameObject.SetActive(s == GameState.Playing && Gm.OverwatchOpen);
            switch (s)
            {
                case GameState.Title:
                    title.gameObject.SetActive(true);
                    continueBtn.gameObject.SetActive(SaveSystem.Exists);
                    break;
                case GameState.Paused: pause.gameObject.SetActive(true); break;
                case GameState.Document: document.gameObject.SetActive(true); break;
                case GameState.Inventory: inventory.gameObject.SetActive(true); selected = -1; combineFrom = -1; RefreshInventory(); break;
                case GameState.ItemBox: itembox.gameObject.SetActive(true); RefreshItemBox(); break;
                case GameState.Map: map.gameObject.SetActive(true); RefreshMap(); break;
                case GameState.GameOver:
                    gameover.gameObject.SetActive(true);
                    goContinueBtn.gameObject.SetActive(SaveSystem.Exists);
                    break;
                case GameState.Victory:
                    victory.gameObject.SetActive(true);
                    victoryStats.text = Loc.F("stats", GameManager.FormatTime(Gm.PlayTime), Gm.Kills, Gm.Saves);
                    victoryRank.text = Loc.F("rank", Gm.Rank());
                    victoryCheats.text = Gm.CheatsUsed ? Loc.Get("cheats_flag") : "";
                    break;
            }
        }

        void Update()
        {
            float now = Time.unscaledTime;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var (t, until) = toasts[i];
                if (t == null) { toasts.RemoveAt(i); continue; }
                float left = until - now;
                var c = t.color; c.a = Mathf.Clamp01(left / 0.5f); t.color = c;
                if (left <= 0) { Destroy(t.transform.parent.gameObject); toasts.RemoveAt(i); }
            }
            if (hitFlash != null)
            {
                var c = hitFlash.color; c.a = Mathf.MoveTowards(c.a, 0, Time.unscaledDeltaTime * 1.5f); hitFlash.color = c;
            }
            if (map != null && map.gameObject.activeSelf) UpdateMapMarker();
        }

        public void Toast(string msg, float dur = 3f)
        {
            if (string.IsNullOrEmpty(msg) || toastArea == null) return;
            var holder = new GameObject("msg", typeof(RectTransform));
            holder.transform.SetParent(toastArea, false);
            ((RectTransform)holder.transform).sizeDelta = new Vector2(1400, 44);
            var bg = UIKit.Img((RectTransform)holder.transform, new Color(0, 0, 0, 0.55f));
            bg.raycastTarget = false;
            var t = UIKit.Label(holder.transform, msg, 28, UIKit.TextC, TextAnchor.MiddleCenter);
            UIKit.Shadow(t);
            toasts.Add((t, Time.unscaledTime + dur));
            while (toasts.Count > 4) { if (toasts[0].text) Destroy(toasts[0].text.transform.parent.gameObject); toasts.RemoveAt(0); }
        }

        public void HitFlash() { if (hitFlash) hitFlash.color = new Color(0.6f, 0, 0, 0.35f); }
        public void ShowObjective() => hud.ShowObjective();
        public void ShowControlsHint() => hud.ShowHint();
        public void ShowOverwatch(bool on) { overwatch.gameObject.SetActive(on); if (on) RefreshOverwatch(); }

        public void ShowDocument(int id)
        {
            docTitle.text = Documents.Title(id);
            docBody.text = Documents.Body(id);
        }

        // ---------------- telas ----------------

        RectTransform Overlay(string name, float alpha)
        {
            var p = UIKit.Fill(root, name);
            UIKit.Img(p, new Color(0.01f, 0.015f, 0.012f, alpha));
            return p;
        }

        RectTransform Menu(RectTransform parent, Vector2 anchor, Vector2 pos, float width)
        {
            var m = UIKit.At(parent, "menu", anchor, pos, new Vector2(width, 420));
            UIKit.VStack(m, 10, 0, anchor.y > 0.5f ? TextAnchor.UpperLeft : TextAnchor.UpperCenter);
            var fit = m.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return m;
        }

        RectTransform BuildTitle()
        {
            var p = UIKit.Fill(root, "Titulo");
            var grad = UIKit.Box(p, "degrade", Vector2.zero, new Vector2(0.65f, 1), Vector2.zero, Vector2.zero);
            UIKit.Img(grad, Color.white, UIKit.Gradient).raycastTarget = false;

            var tbox = UIKit.Box(p, "logo", new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -290), new Vector2(1100, -90));
            var t = UIKit.Label(tbox, "RAVENBROOK", 120, UIKit.Blood, TextAnchor.UpperLeft, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.fontStyle = FontStyle.Bold; UIKit.Shadow(t);
            var sbox = UIKit.Box(p, "sub", new Vector2(0, 1), new Vector2(0, 1), new Vector2(118, -350), new Vector2(1100, -280));
            var s = UIKit.Label(sbox, "— " + Loc.Get("subtitle"), 40, UIKit.Accent, TextAnchor.UpperLeft, true);
            s.fontStyle = FontStyle.Italic;
            var stripe = UIKit.At(p, "listras", new Vector2(0, 1), new Vector2(118, -362), new Vector2(520, 10));
            UIKit.Img(stripe, Color.white, UIKit.Stripes);

            var m = Menu(p, new Vector2(0, 1), new Vector2(118, -410), 440);
            continueBtn = UIKit.Btn(m, Loc.Get("continue"), () => { if (!Gm.ContinueGame()) Toast("—"); });
            UIKit.Btn(m, Loc.Get("new_game"), () => Gm.NewGame());
            UIKit.Btn(m, Loc.Get("options"), () => OpenOptions(GameState.Title));
            UIKit.Btn(m, Loc.Get("credits"), () => { HideMenus(); credits.gameObject.SetActive(true); });
            UIKit.Btn(m, Loc.Get("quit"), Application.Quit);

            var foot = UIKit.Box(p, "rodape", new Vector2(0, 0), new Vector2(1, 0), new Vector2(118, 30), new Vector2(-40, 70));
            UIKit.Label(foot, "Unity 6 · C# · Unity UI · Blender · Sketchfab   —   v0.1", 20, UIKit.Dim, TextAnchor.LowerLeft);
            UIKit.Label(foot, Loc.IsPt ? "Fan game original de survival horror. Sem vínculo com a Capcom." : "Original survival horror fan game. Not affiliated with Capcom.", 18, UIKit.Dim, TextAnchor.LowerRight);
            return p;
        }

        RectTransform BuildPause()
        {
            var p = Overlay("Pausa", 0.78f);
            var tb = UIKit.At(p, "titulo", new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(900, 120));
            UIKit.Label(tb, "RAVENBROOK", 72, UIKit.Blood, TextAnchor.MiddleCenter, true).fontStyle = FontStyle.Bold;
            var m = Menu(p, new Vector2(0.5f, 0.5f), new Vector2(0, 120), 440);
            m.pivot = new Vector2(0.5f, 1f);
            UIKit.Btn(m, Loc.Get("resume"), () => Gm.SetState(GameState.Playing));
            UIKit.Btn(m, Loc.Get("options"), () => OpenOptions(GameState.Paused));
            UIKit.Btn(m, Loc.Get("main_menu"), () => Gm.GoTitle());
            UIKit.Btn(m, Loc.Get("quit"), Application.Quit);
            return p;
        }

        void OpenOptions(GameState from)
        {
            optionsFrom = from;
            HideMenus();
            options.gameObject.SetActive(true);
        }

        void CloseOptions()
        {
            Settings.Save();
            options.gameObject.SetActive(false);
            OnState(optionsFrom);
        }

        RectTransform BuildOptions()
        {
            var p = Overlay("Opcoes", 0.85f);
            var w = UIKit.At(p, "janela", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 720));
            UIKit.Img(w, UIKit.PanelC);
            UIKit.Header(w, Loc.Get("options").ToUpper());
            var body = UIKit.Box(w, "corpo", Vector2.zero, Vector2.one, new Vector2(40, 30), new Vector2(-40, -80));
            UIKit.VStack(body, 12);

            Text Caption(string s)
            {
                var g = new GameObject("rotulo", typeof(RectTransform)); g.transform.SetParent(body, false);
                g.AddComponent<LayoutElement>().preferredHeight = 34;
                return UIKit.Label(g.transform, s, 24, UIKit.Dim, TextAnchor.LowerLeft);
            }

            Caption(Loc.Get("language"));
            var langRow = new GameObject("idioma", typeof(RectTransform)); langRow.transform.SetParent(body, false);
            langRow.AddComponent<LayoutElement>().preferredHeight = 58;
            var hl = langRow.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10; hl.childForceExpandWidth = false; hl.childControlWidth = true;
            var prev = UIKit.Btn(langRow.transform, "  ◄", () => Gm.SetLanguage(Settings.Language - 1));
            prev.GetComponent<LayoutElement>().preferredWidth = 90;
            var nameBox = new GameObject("nome", typeof(RectTransform)); nameBox.transform.SetParent(langRow.transform, false);
            nameBox.AddComponent<LayoutElement>().preferredWidth = 440;
            UIKit.Label(nameBox.transform, Loc.Names[Settings.Language] + $"   ({Settings.Language + 1}/{Loc.Names.Length})", 30, UIKit.TextC, TextAnchor.MiddleCenter);
            var next = UIKit.Btn(langRow.transform, "  ►", () => Gm.SetLanguage(Settings.Language + 1));
            next.GetComponent<LayoutElement>().preferredWidth = 90;

            var sensCap = Caption(Loc.Get("sensitivity") + $": {Settings.Sensitivity:0.0}");
            UIKit.Sld(body, 0.5f, 8f, Settings.Sensitivity, v => { Settings.Sensitivity = v; sensCap.text = Loc.Get("sensitivity") + $": {v:0.0}"; });
            var volCap = Caption(Loc.Get("volume") + $": {Settings.Volume * 100:0}%");
            UIKit.Sld(body, 0f, 1f, Settings.Volume, v => { Settings.Volume = v; AudioListener.volume = v; volCap.text = Loc.Get("volume") + $": {v * 100:0}%"; });

            Caption(Loc.Get("quality"));
            var qRow = new GameObject("qualidade", typeof(RectTransform)); qRow.transform.SetParent(body, false);
            qRow.AddComponent<LayoutElement>().preferredHeight = 58;
            var qh = qRow.AddComponent<HorizontalLayoutGroup>(); qh.spacing = 10; qh.childControlWidth = true; qh.childForceExpandWidth = true;
            var qBtns = new Button[3];
            string[] qKeys = { "low", "medium", "high" };
            for (int i = 0; i < 3; i++)
            {
                int q = i;
                qBtns[i] = UIKit.Btn(qRow.transform, (Settings.Quality == i ? "▶ " : "") + Loc.Get(qKeys[i]), () =>
                {
                    Settings.Quality = q; Settings.ApplyQuality();
                    for (int k = 0; k < 3; k++) UIKit.SetBtnText(qBtns[k], (k == q ? "▶ " : "") + Loc.Get(qKeys[k]));
                });
            }

            Button dev = null;
            dev = UIKit.Btn(body, Loc.Get("dev_mode") + ": " + Loc.Get(Settings.DevMode ? "on" : "off"), () =>
            {
                Settings.DevMode = !Settings.DevMode;
                UIKit.SetBtnText(dev, Loc.Get("dev_mode") + ": " + Loc.Get(Settings.DevMode ? "on" : "off"));
            });
            var spacer = new GameObject("espaco", typeof(RectTransform)); spacer.transform.SetParent(body, false);
            spacer.AddComponent<LayoutElement>().preferredHeight = 10;
            UIKit.Btn(body, Loc.Get("back"), CloseOptions);
            return p;
        }

        RectTransform BuildGameOver()
        {
            var p = Overlay("Morte", 0.8f);
            var tb = UIKit.At(p, "titulo", new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1600, 200));
            var t = UIKit.Label(tb, Loc.Get("you_died"), 130, UIKit.Blood, TextAnchor.MiddleCenter, true);
            t.fontStyle = FontStyle.Bold; UIKit.Shadow(t);
            var m = Menu(p, new Vector2(0.5f, 0.5f), new Vector2(0, 30), 440);
            m.pivot = new Vector2(0.5f, 1f);
            goContinueBtn = UIKit.Btn(m, Loc.Get("continue"), () => Gm.ContinueGame());
            UIKit.Btn(m, Loc.Get("retry"), () => Gm.NewGame());
            UIKit.Btn(m, Loc.Get("main_menu"), () => Gm.GoTitle());
            return p;
        }

        RectTransform BuildVictory()
        {
            var p = Overlay("Vitoria", 0.88f);
            var tb = UIKit.At(p, "titulo", new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1600, 180));
            var t = UIKit.Label(tb, Loc.Get("victory"), 110, UIKit.Accent, TextAnchor.MiddleCenter, true);
            t.fontStyle = FontStyle.Bold; UIKit.Shadow(t);
            var sb = UIKit.At(p, "stats", new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1500, 60));
            victoryStats = UIKit.Label(sb, "", 32, UIKit.TextC, TextAnchor.MiddleCenter);
            var rb = UIKit.At(p, "rank", new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1500, 80));
            victoryRank = UIKit.Label(rb, "", 56, UIKit.Warn, TextAnchor.MiddleCenter, true);
            var cb = UIKit.At(p, "cheats", new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(1500, 40));
            victoryCheats = UIKit.Label(cb, "", 22, UIKit.Dim, TextAnchor.MiddleCenter);
            var m = Menu(p, new Vector2(0.5f, 0.5f), new Vector2(0, -30), 440);
            m.pivot = new Vector2(0.5f, 1f);
            UIKit.Btn(m, Loc.Get("credits"), () => { HideMenus(); credits.gameObject.SetActive(true); });
            UIKit.Btn(m, Loc.Get("main_menu"), () => Gm.GoTitle());
            return p;
        }

        RectTransform BuildCredits()
        {
            var p = Overlay("Creditos", 0.92f);
            var w = UIKit.At(p, "janela", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 860));
            UIKit.Img(w, UIKit.PanelC);
            UIKit.Header(w, Loc.Get("credits").ToUpper());
            var body = UIKit.Box(w, "texto", Vector2.zero, Vector2.one, new Vector2(40, 100), new Vector2(-40, -80));
            string extra = ModelLibrary.Credits();
            string txt = Loc.IsPt
                ? "RAVENBROOK: O SURTO\nFan game original de survival horror, inspirado em Resident Evil. Não usa marcas, nomes nem personagens da Capcom.\n\nFeito com Unity 6 e C# (jogo, IA, mapa, sons e efeitos gerados por código), Unity UI (interface), NavMesh (IA dos inimigos) e Blender 5.2 (modelos gerados por script em Blender/gerar_modelos.py).\n\nModelos do Sketchfab (opcionais, ver TUTORIAL-SKETCHFAB.md):\n"
                : "RAVENBROOK: OUTBREAK\nAn original survival horror fan game inspired by Resident Evil. It uses no Capcom trademarks, names or characters.\n\nMade with Unity 6 and C# (game, AI, map, sounds and effects generated in code), Unity UI (interface), NavMesh (enemy AI) and Blender 5.2 (models generated by script in Blender/gerar_modelos.py).\n\nSketchfab models (optional, see TUTORIAL-SKETCHFAB.md):\n";
            txt += string.IsNullOrWhiteSpace(extra) ? (Loc.IsPt ? "— nenhum modelo externo instalado." : "— no external models installed.") : extra;
            UIKit.Label(body, txt, 24, UIKit.TextC, TextAnchor.UpperLeft);
            var bb = UIKit.At(w, "voltar", new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(380, 58));
            UIKit.VStack(bb, 0);
            UIKit.Btn(bb, Loc.Get("back"), () => OnState(Gm.State));
            return p;
        }

        RectTransform BuildDocument()
        {
            var p = Overlay("Documento", 0.85f);
            var w = UIKit.At(p, "papel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 820));
            UIKit.Img(w, new Color(0.82f, 0.78f, 0.66f, 1f));
            var tb = UIKit.Box(w, "titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -120), new Vector2(-60, -40));
            docTitle = UIKit.Label(tb, "", 40, new Color(0.25f, 0.05f, 0.04f), TextAnchor.MiddleLeft, true);
            docTitle.fontStyle = FontStyle.Bold;
            var line = UIKit.Box(w, "linha", new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -126), new Vector2(-60, -122));
            UIKit.Img(line, new Color(0.3f, 0.1f, 0.08f, 0.6f));
            var bb = UIKit.Box(w, "texto", Vector2.zero, Vector2.one, new Vector2(60, 110), new Vector2(-60, -145));
            docBody = UIKit.Label(bb, "", 27, new Color(0.12f, 0.1f, 0.08f), TextAnchor.UpperLeft, true);
            docBody.lineSpacing = 1.15f;
            var cb = UIKit.At(w, "fechar", new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(380, 58));
            UIKit.VStack(cb, 0);
            UIKit.Btn(cb, Loc.Get("close"), () => Gm.SetState(GameState.Playing));
            return p;
        }
    }
}
