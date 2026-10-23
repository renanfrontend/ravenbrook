using UnityEngine;
using UnityEngine.UI;

namespace Ravenbrook
{
    public partial class UIRoot
    {
        RectTransform inventory, itembox, map;
        int selected = -1, combineFrom = -1;
        SlotView[] invViews, boxInvViews, boxViews;
        Text selName, selDesc, invStatus, invHint;
        Image hpFill;
        RectTransform weaponList, fileList;
        Button useBtn, combineBtn;
        RawImage mapImg; RectTransform mapArrow; Text mapRoom, mapObjective;
        Texture2D mapTex;

        class SlotView
        {
            public Button Btn; public Image Icon; public Text Count; public Image Frame;
        }

        SlotView MakeSlot(Transform parent, Vector2 pos, float size, System.Action onClick)
        {
            var r = UIKit.At(parent, "espaco", new Vector2(0, 1), pos, new Vector2(size, size));
            var bg = UIKit.Img(r, new Color(0.1f, 0.12f, 0.1f, 0.95f));
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var cb = b.colors; cb.highlightedColor = new Color(1.6f, 1.8f, 1.2f); cb.pressedColor = new Color(2f, 2.2f, 1.4f); b.colors = cb;
            b.onClick.AddListener(() => { Sfx.Play("ui"); onClick(); });
            var fr = UIKit.Box(r, "moldura", Vector2.zero, Vector2.one, new Vector2(-3, -3), new Vector2(3, 3));
            var frame = UIKit.Img(fr, new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, 0));
            frame.raycastTarget = false; frame.transform.SetAsFirstSibling();
            var ic = UIKit.Box(r, "icone", Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));
            var icon = UIKit.Img(ic, Color.white); icon.raycastTarget = false; icon.preserveAspect = true;
            var ct = UIKit.Box(r, "qtd", Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-8, -4));
            var count = UIKit.Label(ct, "", 24, UIKit.TextC, TextAnchor.LowerRight);
            UIKit.Shadow(count);
            return new SlotView { Btn = b, Icon = icon, Count = count, Frame = frame };
        }

        static void Show(SlotView v, Slot s, bool sel)
        {
            v.Icon.enabled = !s.Empty;
            if (!s.Empty) v.Icon.sprite = UIKit.Icon(s.Id);
            v.Count.text = !s.Empty && (s.Count > 1 || ItemDB.Get(s.Id).Kind == ItemKind.Ammo) ? s.Count.ToString() : "";
            var c = v.Frame.color; c.a = sel ? 0.5f : 0f; v.Frame.color = c;
        }

        RectTransform BuildInventory()
        {
            var p = UIKit.Fill(root, "Inventario");
            UIKit.Img(p, UIKit.Bg);
            UIKit.Header(p, Loc.Get("inventory").ToUpper() + "   ·   Tab / Esc");

            // coluna esquerda: saúde e armas
            var left = UIKit.Box(p, "esquerda", new Vector2(0, 0), new Vector2(0, 1), new Vector2(60, 60), new Vector2(520, -100));
            var st = UIKit.Box(left, "estado", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), Vector2.zero);
            invStatus = UIKit.Label(st, "", 40, Color.green, TextAnchor.MiddleLeft);
            invStatus.fontStyle = FontStyle.Bold;
            var hb = UIKit.Box(left, "vida", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -84), new Vector2(0, -68));
            UIKit.Img(hb, new Color(0.15f, 0.15f, 0.15f));
            var hf = UIKit.Box(hb, "barra", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hpFill = UIKit.Img(hf, Color.green);
            hpFill.type = Image.Type.Filled; hpFill.fillMethod = Image.FillMethod.Horizontal;
            weaponList = UIKit.Box(left, "armas", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -110));
            UIKit.VStack(weaponList, 8);

            // grade 4 × 2
            var grid = UIKit.Box(p, "grade", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-330, -460), new Vector2(330, -130));
            invViews = new SlotView[8];
            for (int i = 0; i < 8; i++)
            {
                int k = i;
                invViews[i] = MakeSlot(grid, new Vector2((i % 4) * 166, -(i / 4) * 166), 150, () => ClickSlot(k));
            }
            var hintBox = UIKit.Box(p, "dica", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-330, -510), new Vector2(330, -470));
            invHint = UIKit.Label(hintBox, "", 24, UIKit.Warn, TextAnchor.MiddleCenter);

            // detalhes do item
            var right = UIKit.Box(p, "direita", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-560, 60), new Vector2(-60, -130));
            var nb = UIKit.Box(right, "nome", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -56), Vector2.zero);
            selName = UIKit.Label(nb, "", 34, UIKit.Accent, TextAnchor.UpperLeft);
            var db = UIKit.Box(right, "descricao", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -260), new Vector2(0, -64));
            selDesc = UIKit.Label(db, "", 24, UIKit.TextC, TextAnchor.UpperLeft);
            var acts = UIKit.Box(right, "acoes", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -420), new Vector2(0, -280));
            UIKit.VStack(acts, 10);
            useBtn = UIKit.Btn(acts, Loc.Get("use"), UseSelected);
            combineBtn = UIKit.Btn(acts, Loc.Get("combine"), () => { if (selected >= 0) { combineFrom = selected; RefreshInventory(); } });

            // arquivos lidos
            var fl = UIKit.Box(p, "arquivos", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-330, 60), new Vector2(330, 420));
            var ft = UIKit.Box(fl, "titulo", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -36), Vector2.zero);
            UIKit.Label(ft, Loc.Get("files").ToUpper(), 24, UIKit.Dim, TextAnchor.MiddleLeft);
            fileList = UIKit.Box(fl, "lista", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -44));
            UIKit.VStack(fileList, 6);
            return p;
        }

        void ClickSlot(int i)
        {
            var inv = Gm.Player.Inv;
            if (combineFrom >= 0)
            {
                inv.Combine(combineFrom, i, out string msg);
                Toast(msg);
                combineFrom = -1; selected = inv.Slots[i].Empty ? -1 : i;
            }
            else selected = inv.Slots[i].Empty ? -1 : i;
            RefreshInventory();
        }

        void UseSelected()
        {
            if (selected < 0) return;
            var msg = Gm.Player.Inv.Use(selected, Gm.Player);
            if (!string.IsNullOrEmpty(msg)) Toast(msg);
            if (Gm.Player.Inv.Slots[selected].Empty) selected = -1;
            RefreshInventory();
        }

        void RefreshInventory()
        {
            var p = Gm.Player; if (p == null) return;
            var inv = p.Inv;
            for (int i = 0; i < 8; i++) Show(invViews[i], inv.Slots[i], i == selected || i == combineFrom);
            invStatus.text = "♥ " + Loc.Get(p.StatusKey); invStatus.color = p.StatusColor;
            hpFill.fillAmount = p.Hp / p.MaxHp; hpFill.color = p.StatusColor;

            bool has = selected >= 0 && !inv.Slots[selected].Empty;
            var id = has ? inv.Slots[selected].Id : ItemId.None;
            selName.text = has ? ItemDB.Name(id) : "";
            selDesc.text = has ? ItemDB.Desc(id) : "";
            useBtn.gameObject.SetActive(has && ItemDB.Get(id).Kind == ItemKind.Heal);
            combineBtn.gameObject.SetActive(has && (ItemDB.Get(id).Kind == ItemKind.Heal || ItemDB.Get(id).Kind == ItemKind.Ammo));
            invHint.text = combineFrom >= 0 ? Loc.Get("choose_combine") : "";

            for (int i = weaponList.childCount - 1; i >= 0; i--) Destroy(weaponList.GetChild(i).gameObject);
            var ws = p.Weapons;
            for (int w = 0; w < WeaponSystem.Defs.Length; w++)
            {
                if (!ws.Owned[w]) continue;
                int k = w;
                var d = WeaponSystem.Defs[w];
                string info = d.Ammo == ItemId.None ? $"{Loc.Get("durability")} {ws.KnifeDur:0}%" : $"{ws.Mag[w]} / {ws.Reserve(w)}";
                UIKit.Btn(weaponList, (ws.Current == w ? "▶ " : "") + $"{w + 1}. {Loc.Get(d.Key)}   ·   {info}", () => { ws.Select(k); RefreshInventory(); }, 24, 52);
            }

            for (int i = fileList.childCount - 1; i >= 0; i--) Destroy(fileList.GetChild(i).gameObject);
            foreach (int doc in Gm.DocsRead)
            {
                int k = doc;
                UIKit.Btn(fileList, Documents.Title(doc), () => { ShowDocument(k); Gm.SetState(GameState.Document); }, 22, 46);
            }
        }

        // ---------------- baú ----------------

        RectTransform BuildItemBox()
        {
            var p = UIKit.Fill(root, "Bau");
            UIKit.Img(p, UIKit.Bg);
            UIKit.Header(p, Loc.Get("item_box").ToUpper() + "   ·   Esc");
            var lt = UIKit.Box(p, "t1", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-780, -150), new Vector2(-120, -110));
            UIKit.Label(lt, Loc.Get("inventory") + "  →  " + Loc.Get("store"), 26, UIKit.Dim, TextAnchor.MiddleLeft);
            var rt = UIKit.Box(p, "t2", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(60, -150), new Vector2(780, -110));
            UIKit.Label(rt, Loc.Get("item_box") + "  →  " + Loc.Get("withdraw"), 26, UIKit.Dim, TextAnchor.MiddleLeft);
            var lg = UIKit.Box(p, "inv", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-780, -800), new Vector2(-120, -170));
            var rg = UIKit.Box(p, "bau", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(60, -800), new Vector2(780, -170));
            boxInvViews = new SlotView[8]; boxViews = new SlotView[16];
            for (int i = 0; i < 8; i++)
            {
                int k = i;
                boxInvViews[i] = MakeSlot(lg, new Vector2((i % 4) * 160, -(i / 4) * 160), 145, () => { Gm.Player.Inv.ToBox(k); RefreshItemBox(); });
            }
            for (int i = 0; i < 16; i++)
            {
                int k = i;
                boxViews[i] = MakeSlot(rg, new Vector2((i % 4) * 160, -(i / 4) * 160), 145, () => { if (!Gm.Player.Inv.FromBox(k)) Toast(Loc.Get("inv_full")); RefreshItemBox(); });
            }
            var cb = UIKit.At(p, "fechar", new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(380, 58));
            UIKit.VStack(cb, 0);
            UIKit.Btn(cb, Loc.Get("close"), () => Gm.SetState(GameState.Playing));
            return p;
        }

        void RefreshItemBox()
        {
            var inv = Gm.Player.Inv;
            for (int i = 0; i < 8; i++) Show(boxInvViews[i], inv.Slots[i], false);
            for (int i = 0; i < 16; i++) Show(boxViews[i], inv.Box[i], false);
        }

        // ---------------- mapa ----------------

        RectTransform BuildMap()
        {
            var p = UIKit.Fill(root, "Mapa");
            UIKit.Img(p, UIKit.Bg);
            UIKit.Header(p, Loc.Get("map").ToUpper() + "   ·   M / Esc");
            var mb = UIKit.At(p, "planta", new Vector2(0.5f, 0.5f), new Vector2(-180, -20), new Vector2(31 * 26, 25 * 26));
            mapImg = mb.gameObject.AddComponent<RawImage>();
            mapImg.raycastTarget = false;
            for (int room = 0; room < 9; room++)
            {
                int bx = (room % 3) * 10 + 5, br = (room / 3) * 8 + 4;
                var lb = UIKit.At(mb, "sala", new Vector2(0, 1), new Vector2(bx * 26 + 13 - 150, -(br * 26 + 13) + 18), new Vector2(300, 36));
                var t = UIKit.Label(lb, Loc.Get(LevelBuilder.RoomKeys[room]), 22, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
                UIKit.Shadow(t);
            }
            mapArrow = UIKit.At(mb, "jogador", new Vector2(0, 1), Vector2.zero, new Vector2(26, 26));
            mapArrow.pivot = new Vector2(0.5f, 0.5f);
            UIKit.Img(mapArrow, new Color(1f, 0.9f, 0.2f), UIKit.Arrow);

            var side = UIKit.Box(p, "legenda", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-440, -300), new Vector2(-60, 300));
            UIKit.VStack(side, 14);
            mapRoom = AddLine(side, "", 32, UIKit.Accent);
            mapObjective = AddLine(side, "", 22, UIKit.TextC);
            AddLegend(side, new Color(0.55f, 0.13f, 0.1f), Loc.Get("legend_visited"));
            AddLegend(side, new Color(0.17f, 0.32f, 0.55f), Loc.Get("legend_cleared"));
            AddLegend(side, new Color(0.95f, 0.25f, 0.2f), Loc.Get("legend_locked"));
            AddLegend(side, new Color(0.3f, 0.75f, 0.35f), Loc.Get("save") + " / " + Loc.Get("item_box"));
            return p;
        }

        Text AddLine(Transform parent, string s, int size, Color c)
        {
            var g = new GameObject("linha", typeof(RectTransform)); g.transform.SetParent(parent, false);
            g.AddComponent<LayoutElement>().preferredHeight = size * 2.2f;
            return UIKit.Label(g.transform, s, size, c, TextAnchor.UpperLeft);
        }

        void AddLegend(Transform parent, Color c, string s)
        {
            var g = new GameObject("item", typeof(RectTransform)); g.transform.SetParent(parent, false);
            g.AddComponent<LayoutElement>().preferredHeight = 34;
            var sq = UIKit.At(g.transform, "cor", new Vector2(0, 0.5f), Vector2.zero, new Vector2(26, 26));
            UIKit.Img(sq, c);
            var t = UIKit.Label(g.transform, s, 22, UIKit.TextC, TextAnchor.MiddleLeft);
            t.rectTransform.offsetMin = new Vector2(40, 0);
        }

        void RefreshMap()
        {
            var lvl = Gm.Level; if (lvl == null) return;
            int W = lvl.W, H = lvl.H;
            if (mapTex == null) mapTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var remaining = new bool[9];
            foreach (var pk in lvl.Pickups) if (pk != null && pk.Room >= 0) remaining[pk.Room] = true;
            for (int r = 0; r < H; r++)
                for (int x = 0; x < W; x++)
                {
                    char c = lvl.G[x, r];
                    int room = LevelBuilder.RoomOf(x, r);
                    Color col;
                    if (c == '#') col = new Color(0.04f, 0.05f, 0.045f, 1f);
                    else if (room < 0) col = new Color(0.3f, 0.32f, 0.3f);
                    else if (!Gm.Flags.Contains("v" + room)) col = new Color(0.2f, 0.22f, 0.2f);
                    else col = remaining[room] ? new Color(0.55f, 0.13f, 0.1f) : new Color(0.17f, 0.32f, 0.55f);
                    if (c == 'T' || c == 'I') col = new Color(0.3f, 0.75f, 0.35f);
                    if (c == 'S') col = new Color(0.8f, 0.75f, 0.5f);
                    mapTex.SetPixel(x, H - 1 - r, col);
                }
            foreach (var d in lvl.Doors)
            {
                Color col = d.Open ? new Color(0.55f, 0.75f, 0.95f) : d.Locked ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.75f, 0.7f, 0.55f);
                mapTex.SetPixel(d.CellX, H - 1 - d.CellR, col);
            }
            mapTex.Apply();
            mapImg.texture = mapTex;
            int cur = lvl.RoomAt(Gm.Player.transform.position);
            mapRoom.text = cur >= 0 ? Loc.Get(LevelBuilder.RoomKeys[cur]) : "—";
            mapObjective.text = Loc.Get("objective") + ": " + Gm.ObjectiveText();
            UpdateMapMarker();
        }

        void UpdateMapMarker()
        {
            var p = Gm.Player; var lvl = Gm.Level;
            if (p == null || lvl == null) return;
            Vector3 pos = p.transform.position;
            float cx = pos.x / LevelBuilder.Cell, cr = lvl.H - 1 - pos.z / LevelBuilder.Cell;
            mapArrow.anchoredPosition = new Vector2(cx * 26 + 13, -(cr * 26 + 13));
            mapArrow.localRotation = Quaternion.Euler(0, 0, -p.transform.eulerAngles.y);
            float blink = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
            mapArrow.GetComponent<Image>().color = new Color(1f, 0.9f, 0.2f, blink);
        }
    }
}
