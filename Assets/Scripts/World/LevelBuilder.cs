using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Ravenbrook
{
    /// <summary>
    /// Monta a Delegacia de Ravenbrook a partir de um mapa em texto (cada caractere = 2,5 m).
    ///
    /// Legenda: # parede · . chão · P início · L lâmpada · d porta · A porta do Corvo · C porta do Leão
    /// G portão da garagem · E saída · S estátua · T máquina de escrever · I baú · c caixa · k mesa · x barricada
    /// Z zumbi · z zumbi caído · R corredor · h erva verde · r erva vermelha · f spray · p/g/n munição
    /// s escopeta · M magnum · a Chave do Corvo · b Chave do Leão · m medalhão · F documento
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public const float Cell = 2.5f, WallH = 3.4f, DoorW = 1.6f;

        // 3 × 3 salas de 9 × 7 células. Ordem: linha de cima (norte) para baixo.
        static readonly string[][] Blocks =
        {
            new[] { "c.......c", ".k..Z..k.", "....L....", ".k..s..k.", "..z......", ".k..F..k.", "c.......c" }, // 0 Arquivo
            new[] { ".........", ".##.R.##.", "....L....", ".##.m.##.", "......R..", ".##...##.", "..p......" }, // 1 Biblioteca
            new[] { "c..h....c", ".........", "..Z.L..Z.", "....m....", ".k.....k.", "..g...r..", "x.......x" }, // 2 Registros
            new[] { "k.k...k.k", ".........", "..Z.L..Z.", "...b.....", ".k..m..k.", "..Z......", "F.......c" }, // 3 Escritório oeste
            new[] { ".........", ".#.....#.", "....L....", "....S....", ".........", ".#..P..#.", "........." }, // 4 Saguão
            new[] { "k.k...k.k", "..Z......", "....L..Z.", ".......a.", ".k..p..k.", "..Z......", "c..F....h" }, // 5 Escritório leste
            new[] { ".........", ".#.#.#.#.", "..Z...Z..", "....L....", ".........", ".#.#.#.#.", "M.z..f..n" }, // 6 Celas
            new[] { ".........", ".T.....I.", "....L....", "..h...p..", ".........", ".F.......", "c.......c" }, // 7 Sala segura
            new[] { ".........", "..Z...Z..", "....L....", ".cc.p.cc.", "....R....", ".........", "...xEx..." }, // 8 Garagem
        };

        public static readonly string[] RoomKeys =
            { "room_archive", "room_library", "room_records", "room_west", "room_lobby", "room_east", "room_cells", "room_safe", "room_garage" };

        public static readonly string[] Map = ComposeMap();

        static string[] ComposeMap()
        {
            var rows = new List<string> { new string('#', 31) };
            for (int br = 0; br < 3; br++)
            {
                for (int i = 0; i < 7; i++)
                {
                    char s0 = br == 1 && i == 3 ? 'A' : '#';
                    char s1 = br == 1 && i == 3 ? 'd' : '#';
                    rows.Add("#" + Blocks[br * 3][i] + s0 + Blocks[br * 3 + 1][i] + s1 + Blocks[br * 3 + 2][i] + "#");
                }
                if (br == 0) rows.Add("#####d#########C#########d#####");
                if (br == 1) rows.Add("#####d#########d#########G#####");
            }
            rows.Add(new string('#', 31));
            return rows.ToArray();
        }

        public int W, H;
        public char[,] G;
        public Vector3 PlayerStart; public float StartYaw;
        public Vector3 ExitPos;
        public Statue StatueObj;
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<Door> Doors = new List<Door>();

        Transform statics, dynamics;
        Material wallM, floorM, ceilM, woodM, darkWoodM, metalM, darkM, paperM, crateM;
        readonly List<(ZType type, Vector3 pos, string id, bool dormant)> zombies = new List<(ZType, Vector3, string, bool)>();
        System.Random rnd;

        public static LevelBuilder Build(HashSet<string> flags)
        {
            var root = new GameObject("Delegacia");
            var lb = root.AddComponent<LevelBuilder>();
            lb.Generate(flags);
            return lb;
        }

        public Vector3 CellPos(int x, int r) => new Vector3(x * Cell, 0, (H - 1 - r) * Cell);

        public bool ToCell(Vector3 p, out int x, out int r)
        {
            x = Mathf.RoundToInt(p.x / Cell);
            r = H - 1 - Mathf.RoundToInt(p.z / Cell);
            return x >= 0 && x < W && r >= 0 && r < H;
        }

        public static int RoomOf(int x, int r)
        {
            int bc = x >= 1 && x <= 9 ? 0 : x >= 11 && x <= 19 ? 1 : x >= 21 && x <= 29 ? 2 : -1;
            int br = r >= 1 && r <= 7 ? 0 : r >= 9 && r <= 15 ? 1 : r >= 17 && r <= 23 ? 2 : -1;
            return bc < 0 || br < 0 ? -1 : br * 3 + bc;
        }

        public int RoomAt(Vector3 p) => ToCell(p, out int x, out int r) ? RoomOf(x, r) : -1;
        public bool IsSafe(Vector3 p) => RoomAt(p) == 7;

        /// <summary>Ponto livre perto do centro da sala (para teletransporte).</summary>
        public Vector3 RoomPoint(int room) => CellPos((room % 3) * 10 + 5, (room / 3) * 8 + 5);

        void Generate(HashSet<string> flags)
        {
            H = Map.Length; W = Map[0].Length;
            G = new char[W, H];
            for (int r = 0; r < H; r++)
            {
                if (Map[r].Length != W) Debug.LogError($"[Mapa] linha {r} tem {Map[r].Length} caracteres (esperado {W})");
                for (int x = 0; x < W; x++) G[x, r] = x < Map[r].Length ? Map[r][x] : '#';
            }
            rnd = new System.Random(1337);
            statics = new GameObject("Estatico").transform; statics.SetParent(transform, false);
            dynamics = new GameObject("Dinamico").transform; dynamics.SetParent(transform, false);

            wallM = Mats.World(new Color(0.78f, 0.74f, 0.64f), new Color(0.22f, 0.3f, 0.26f), 1.1f, Mats.Plaster(), 0.45f, 0.18f, 0.6f);
            floorM = Mats.World(Color.white, Color.white, -100f, Mats.FloorTiles(), 0.5f, 0.55f, 0f);
            ceilM = Mats.World(new Color(0.45f, 0.45f, 0.43f), Color.white, -100f, Mats.Concrete(), 0.3f, 0.05f, 0f);
            woodM = Mats.Lit(new Color(0.55f, 0.38f, 0.24f), 0.25f, 0f, Mats.Wood());
            darkWoodM = Mats.Lit(new Color(0.3f, 0.19f, 0.12f), 0.3f, 0f, Mats.Wood());
            crateM = Mats.Lit(new Color(0.62f, 0.5f, 0.32f), 0.15f, 0f, Mats.Wood());
            metalM = Mats.Lit(new Color(0.42f, 0.45f, 0.47f), 0.5f, 0.7f);
            darkM = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.35f, 0.3f);
            paperM = Mats.Lit(new Color(0.85f, 0.82f, 0.72f), 0.05f);

            BuildShell();

            int doc = 0, medal = 0;
            for (int r = 0; r < H; r++)
                for (int x = 0; x < W; x++)
                {
                    char c = G[x, r];
                    Vector3 p = CellPos(x, r);
                    string id = x + "_" + r;
                    switch (c)
                    {
                        case 'd': case 'A': case 'C': case 'G': BuildDoor(x, r, c, flags); break;
                        case 'L': BuildLamp(p, RoomOf(x, r)); break;
                        case 'c': BuildCrate(p); break;
                        case 'k': BuildDesk(p); break;
                        case 'x': BuildBarricade(p); break;
                        case 'S': BuildStatue(p); break;
                        case 'T': BuildTypewriter(p); break;
                        case 'I': BuildItemBox(p); break;
                        case 'P': PlayerStart = p; StartYaw = 0f; break;
                        case 'E': BuildExit(p); break;
                        case 'Z': if (!flags.Contains("z" + id)) zombies.Add((ZType.Walker, p, "z" + id, false)); break;
                        case 'z': if (!flags.Contains("z" + id)) zombies.Add((ZType.Walker, p, "z" + id, true)); break;
                        case 'R': if (!flags.Contains("z" + id)) zombies.Add((ZType.Runner, p, "z" + id, false)); break;
                        case 'h': AddPickup(x, r, flags, ItemId.HerbG, 1); break;
                        case 'r': AddPickup(x, r, flags, ItemId.HerbR, 1); break;
                        case 'f': AddPickup(x, r, flags, ItemId.Spray, 1); break;
                        case 'p': AddPickup(x, r, flags, ItemId.AmmoP, 12); break;
                        case 'g': AddPickup(x, r, flags, ItemId.AmmoS, 6); break;
                        case 'n': AddPickup(x, r, flags, ItemId.AmmoM, 4); break;
                        case 'a': AddPickup(x, r, flags, ItemId.KeyCrow, 1); break;
                        case 'b': AddPickup(x, r, flags, ItemId.KeyLion, 1); break;
                        case 'm':
                            AddPickup(x, r, flags, medal == 0 ? ItemId.MedalSun : medal == 1 ? ItemId.MedalMoon : ItemId.MedalStar, 1);
                            medal++; break;
                        case 's': AddPickup(x, r, flags, ItemId.None, 1, weapon: (int)WeaponId.Shotgun, loaded: 4); break;
                        case 'M': AddPickup(x, r, flags, ItemId.None, 1, weapon: (int)WeaponId.Magnum, loaded: 3); break;
                        case 'F': AddPickup(x, r, flags, ItemId.None, 1, doc: doc); doc++; break;
                    }
                }

            BuildDecor();
            ScatterBlood();
            ApplyEnvironment();

            // NavMesh só com a geometria fixa (portas ficam de fora e bloqueiam com NavMeshObstacle)
            var surf = gameObject.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.Children;
            surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.layerMask = 1 << Layers.Default;
            surf.BuildNavMesh();

            foreach (var z in zombies)
                Zombie.Spawn(z.type, z.pos, (float)rnd.NextDouble() * 360f, z.id, z.dormant, dynamics);
            if (flags.Contains("brute") && !flags.Contains("gate_used")) SpawnBrute();
            if (StatueObj != null) StatueObj.Refresh();

            StaticBatchingUtility.Combine(statics.gameObject);
        }

        public Zombie SpawnBrute() => Zombie.Spawn(ZType.Brute, CellPos(13, 9), 180f, "brute", false, dynamics);

        // ---------------- estrutura ----------------

        GameObject Block(Transform parent, Vector3 center, Vector3 size, Material m, bool collider, string name = "bloco")
        {
            var g = Mats.Part(parent, PrimitiveType.Cube, center, size, m, name);
            if (collider) g.AddComponent<BoxCollider>();
            return g;
        }

        void BuildShell()
        {
            for (int cx = 0; cx < W; cx += 3)
                for (int cr = 0; cr < H; cr += 3)
                {
                    int nx = Mathf.Min(3, W - cx), nr = Mathf.Min(3, H - cr);
                    Vector3 c = new Vector3((cx + (nx - 1) / 2f) * Cell, 0, (H - 1 - (cr + (nr - 1) / 2f)) * Cell);
                    Block(statics, c + Vector3.down * 0.1f, new Vector3(nx * Cell, 0.2f, nr * Cell), floorM, true, "piso");
                    var ceil = Block(statics, c + Vector3.up * (WallH + 0.1f), new Vector3(nx * Cell, 0.2f, nr * Cell), ceilM, false, "teto");
                    ceil.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }

            for (int r = 0; r < H; r++)
            {
                int x = 0;
                while (x < W)
                {
                    if (G[x, r] != '#') { x++; continue; }
                    int len = 0;
                    while (x + len < W && G[x + len, r] == '#' && len < 4) len++;
                    Vector3 a = CellPos(x, r), b = CellPos(x + len - 1, r);
                    Block(statics, (a + b) * 0.5f + Vector3.up * WallH * 0.5f, new Vector3(len * Cell, WallH, Cell), wallM, true, "parede");
                    x += len;
                }
            }
        }

        void BuildDoor(int x, int r, char c, HashSet<string> flags)
        {
            Vector3 p = CellPos(x, r);
            bool alongX = G[x - 1, r] == '#' && G[x + 1, r] == '#';
            var rot = Quaternion.Euler(0, alongX ? 0 : 90, 0);

            // batentes e verga (fixos, entram na NavMesh)
            var frame = new GameObject("batente").transform;
            frame.SetParent(statics, false); frame.SetPositionAndRotation(p, rot);
            float postW = (Cell - DoorW) / 2f;
            foreach (float s in new[] { -1f, 1f })
                Block(frame, new Vector3(s * (DoorW + postW) / 2f, WallH / 2f, 0), new Vector3(postW, WallH, Cell), wallM, true, "batente");
            Block(frame, new Vector3(0, 2.5f + (WallH - 2.5f) / 2f, 0), new Vector3(DoorW, WallH - 2.5f, Cell), wallM, true, "verga");
            foreach (float z in new[] { -Cell / 2f, Cell / 2f })
            {
                Block(frame, new Vector3(0, 2.55f, z), new Vector3(DoorW + 0.2f, 0.1f, 0.06f), darkWoodM, false, "moldura");
                foreach (float s in new[] { -1f, 1f })
                    Block(frame, new Vector3(s * (DoorW / 2f + 0.05f), 1.25f, z), new Vector3(0.1f, 2.5f, 0.06f), darkWoodM, false, "moldura");
            }

            var go = new GameObject("Porta_" + x + "_" + r);
            go.transform.SetParent(dynamics, false);
            go.transform.SetPositionAndRotation(p, rot);
            var d = go.AddComponent<Door>();
            d.Id = "d" + x + "_" + r; d.CellX = x; d.CellR = r;
            d.Gate = c == 'G';
            d.Key = c == 'A' ? ItemId.KeyCrow : c == 'C' ? ItemId.KeyLion : ItemId.None;
            bool open = flags.Contains(d.Id) || (d.Gate && flags.Contains("gate"));
            var mat = d.Gate ? metalM : c == 'd' ? woodM : darkWoodM;
            d.Build(DoorW, mat, d.Gate ? darkM : Mats.Lit(new Color(0.75f, 0.62f, 0.3f), 0.8f, 0.9f), open);
            Doors.Add(d);
        }

        void BuildLamp(Vector3 p, int room)
        {
            var g = new GameObject("lampada").transform;
            g.SetParent(dynamics, false);
            g.position = p + Vector3.up * (WallH - 0.02f);
            bool safe = room == 7;
            Color lc = safe ? new Color(1f, 0.78f, 0.5f) : new Color(1f, 0.88f, 0.68f);
            Mats.Part(g, PrimitiveType.Cylinder, new Vector3(0, -0.05f, 0), new Vector3(0.7f, 0.04f, 0.7f), darkM, "luminaria");
            Mats.Part(g, PrimitiveType.Sphere, new Vector3(0, -0.12f, 0), new Vector3(0.4f, 0.12f, 0.4f), Mats.Emissive(lc, lc * 1.6f), "bulbo");
            var lg = new GameObject("luz");
            lg.transform.SetParent(g, false);
            lg.transform.localPosition = new Vector3(0, -0.4f, 0);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = safe ? 13f : 12.5f; l.intensity = safe ? 2.6f : 2.2f; l.color = lc;
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
            if (!safe && rnd.NextDouble() < 0.45) lg.AddComponent<FlickerLight>();
        }

        void Collide(Transform holder, Vector3 center, Vector3 size)
        {
            var bc = holder.gameObject.AddComponent<BoxCollider>();
            bc.center = center; bc.size = size;
        }

        Transform Holder(string name, Vector3 p, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(statics, false);
            t.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            return t;
        }

        float Jitter(float baseYaw) => baseYaw + (float)(rnd.NextDouble() * 16 - 8);

        void BuildCrate(Vector3 p)
        {
            var h = Holder("caixa", p + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) * 0.6f, Jitter(rnd.Next(4) * 90));
            if (ModelLibrary.Spawn("Caixa", h, Vector3.zero, 1.05f) == null)
            {
                Block(h, new Vector3(0, 0.5f, 0), Vector3.one, crateM, false, "caixa");
                foreach (float y in new[] { 0.05f, 0.95f })
                    Block(h, new Vector3(0, y, 0), new Vector3(1.02f, 0.08f, 1.02f), darkWoodM, false, "ripa");
                if (rnd.NextDouble() < 0.5) Block(h, new Vector3(0.05f, 1.3f, 0.05f), new Vector3(0.6f, 0.6f, 0.6f), crateM, false, "caixa");
            }
            Collide(h, new Vector3(0, 0.6f, 0), new Vector3(1.05f, 1.2f, 1.05f));
        }

        void BuildDesk(Vector3 p)
        {
            var h = Holder("mesa", p, Jitter(rnd.Next(2) * 90));
            if (ModelLibrary.Spawn("Mesa", h, Vector3.zero, 1.6f) == null)
            {
                Block(h, new Vector3(0, 0.76f, 0), new Vector3(1.6f, 0.06f, 0.8f), woodM, false, "tampo");
                foreach (var lx in new[] { -0.74f, 0.74f })
                    foreach (var lz in new[] { -0.34f, 0.34f })
                        Block(h, new Vector3(lx, 0.37f, lz), new Vector3(0.06f, 0.74f, 0.06f), darkM, false, "perna");
                Block(h, new Vector3(0.45f, 0.5f, 0), new Vector3(0.6f, 0.45f, 0.72f), darkWoodM, false, "gaveteiro");
            }
            // monitor velho e papéis
            Block(h, new Vector3(-0.3f, 0.98f, 0.05f), new Vector3(0.42f, 0.36f, 0.38f), Mats.Lit(new Color(0.75f, 0.72f, 0.62f), 0.2f), false, "monitor");
            Block(h, new Vector3(-0.3f, 0.99f, -0.145f), new Vector3(0.34f, 0.26f, 0.01f), Mats.Emissive(new Color(0.05f, 0.08f, 0.06f), new Color(0.02f, 0.06f, 0.03f)), false, "tela");
            Block(h, new Vector3(0.35f, 0.795f, -0.1f), new Vector3(0.3f, 0.01f, 0.4f), paperM, false, "papel");
            Collide(h, new Vector3(0, 0.45f, 0), new Vector3(1.65f, 0.9f, 0.85f));
        }

        void BuildBarricade(Vector3 p)
        {
            var h = Holder("barricada", p, Jitter(rnd.Next(2) * 90));
            if (ModelLibrary.Spawn("Barricada", h, Vector3.zero, 2.0f) == null)
            {
                foreach (var x in new[] { -0.8f, 0.8f }) Block(h, new Vector3(x, 0.6f, 0), new Vector3(0.12f, 1.2f, 0.12f), darkWoodM, false, "poste");
                Block(h, new Vector3(0, 0.35f, 0.08f), new Vector3(2f, 0.18f, 0.05f), woodM, false, "tabua").transform.localRotation = Quaternion.Euler(0, 0, 8);
                Block(h, new Vector3(0, 0.75f, 0.08f), new Vector3(2f, 0.18f, 0.05f), woodM, false, "tabua").transform.localRotation = Quaternion.Euler(0, 0, -12);
                Block(h, new Vector3(0, 1.05f, 0.08f), new Vector3(1.9f, 0.16f, 0.05f), woodM, false, "tabua").transform.localRotation = Quaternion.Euler(0, 0, 4);
            }
            Collide(h, new Vector3(0, 0.6f, 0), new Vector3(2f, 1.2f, 0.4f));
        }

        void BuildStatue(Vector3 p)
        {
            var h = Holder("estatua", p, 180f);
            var stone = Mats.Lit(new Color(0.55f, 0.55f, 0.52f), 0.2f, 0f, Mats.Concrete());
            if (ModelLibrary.Spawn("Estatua", h, Vector3.zero, 3.0f) == null)
            {
                Mats.Part(h, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(1.5f, 0.5f, 1.5f), stone, "pedestal");
                Mats.Part(h, PrimitiveType.Cylinder, new Vector3(0, 1.05f, 0), new Vector3(1.1f, 0.05f, 1.1f), stone, "borda");
                Mats.Part(h, PrimitiveType.Capsule, new Vector3(0, 1.85f, 0), new Vector3(0.55f, 0.75f, 0.45f), stone, "figura");
                Mats.Part(h, PrimitiveType.Sphere, new Vector3(0, 2.75f, 0), new Vector3(0.32f, 0.36f, 0.32f), stone, "cabeca");
                Mats.Part(h, PrimitiveType.Capsule, new Vector3(0.38f, 2.55f, 0.05f), new Vector3(0.16f, 0.5f, 0.16f), stone, "braco", new Vector3(0, 0, -25));
                Mats.Part(h, PrimitiveType.Cylinder, new Vector3(0.55f, 3.05f, 0.05f), new Vector3(0.12f, 0.12f, 0.12f), metalM, "tocha");
            }
            var st = h.gameObject.AddComponent<Statue>();
            for (int i = 0; i < 3; i++)
            {
                float a = (i - 1) * 32f * Mathf.Deg2Rad;
                Vector3 sp = new Vector3(Mathf.Sin(a) * 0.76f, 0.62f, Mathf.Cos(a) * 0.76f);
                Mats.Part(h, PrimitiveType.Cylinder, sp, new Vector3(0.24f, 0.02f, 0.24f), darkM, "encaixe", new Vector3(90, a * Mathf.Rad2Deg, 0));
                var medal = (ItemId)((int)ItemId.MedalSun + i);
                st.Sockets[i] = Mats.Part(h, PrimitiveType.Cylinder, sp + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 0.02f, new Vector3(0.2f, 0.02f, 0.2f),
                    Mats.Emissive(ItemDB.Get(medal).Color, ItemDB.Get(medal).Color * 0.4f), "medalhao", new Vector3(90, a * Mathf.Rad2Deg, 0));
            }
            StatueObj = st;
            Collide(h, new Vector3(0, 1.5f, 0), new Vector3(1.5f, 3f, 1.5f));
        }

        void BuildTypewriter(Vector3 p)
        {
            var h = Holder("maquina", p, 180f);
            Block(h, new Vector3(0, 0.38f, 0), new Vector3(0.9f, 0.76f, 0.6f), darkWoodM, false, "mesinha");
            if (ModelLibrary.Spawn("MaquinaEscrever", h, new Vector3(0, 0.76f, 0), 0.5f) == null)
            {
                Block(h, new Vector3(0, 0.84f, 0), new Vector3(0.45f, 0.14f, 0.34f), darkM, false, "corpo");
                Block(h, new Vector3(0, 0.82f, -0.12f), new Vector3(0.38f, 0.04f, 0.12f), Mats.Lit(new Color(0.8f, 0.78f, 0.7f), 0.3f), false, "teclas");
                Mats.Part(h, PrimitiveType.Cylinder, new Vector3(0, 0.95f, 0.08f), new Vector3(0.06f, 0.25f, 0.06f), metalM, "rolo", new Vector3(0, 0, 90));
                Block(h, new Vector3(0, 1.05f, 0.1f), new Vector3(0.24f, 0.2f, 0.01f), paperM, false, "folha").transform.localRotation = Quaternion.Euler(-15, 0, 0);
            }
            h.gameObject.AddComponent<Typewriter>();
            Collide(h, new Vector3(0, 0.45f, 0), new Vector3(0.95f, 0.9f, 0.65f));
            // luminária de mesa: a sala segura é acolhedora
            var lg = new GameObject("abajur"); lg.transform.SetParent(h, false); lg.transform.localPosition = new Vector3(0.3f, 1.3f, 0.2f);
            var l = lg.AddComponent<Light>(); l.renderMode = LightRenderMode.ForcePixel; l.type = LightType.Point; l.range = 4f; l.intensity = 1.2f; l.color = new Color(1f, 0.7f, 0.4f);
        }

        void BuildItemBox(Vector3 p)
        {
            var h = Holder("bau", p, 180f);
            if (ModelLibrary.Spawn("Bau", h, Vector3.zero, 1.1f) == null)
            {
                Block(h, new Vector3(0, 0.3f, 0), new Vector3(1.1f, 0.6f, 0.6f), darkWoodM, false, "caixa");
                Block(h, new Vector3(0, 0.66f, 0), new Vector3(1.12f, 0.12f, 0.62f), woodM, false, "tampa");
                foreach (var x in new[] { -0.4f, 0.4f }) Block(h, new Vector3(x, 0.35f, 0), new Vector3(0.06f, 0.74f, 0.64f), metalM, false, "cinta");
                Block(h, new Vector3(0, 0.5f, -0.31f), new Vector3(0.12f, 0.14f, 0.02f), Mats.Lit(new Color(0.8f, 0.65f, 0.3f), 0.8f, 0.9f), false, "fecho");
            }
            h.gameObject.AddComponent<ItemBoxObj>();
            Collide(h, new Vector3(0, 0.35f, 0), new Vector3(1.15f, 0.75f, 0.65f));
        }

        void BuildExit(Vector3 p)
        {
            ExitPos = p;
            // porta de enrolar da garagem e placa de saída na parede sul
            Vector3 wall = p + Vector3.back * (Cell * 0.5f - 0.02f);
            var h = Holder("saida", wall, 0f);
            Block(h, new Vector3(0, 1.4f, 0), new Vector3(4f, 2.8f, 0.08f), metalM, false, "porta_rolo");
            for (int i = 0; i < 12; i++) Block(h, new Vector3(0, 0.15f + i * 0.22f, 0.05f), new Vector3(4f, 0.03f, 0.03f), darkM, false, "friso");
            Block(h, new Vector3(0, 3.0f, 0.08f), new Vector3(0.9f, 0.28f, 0.1f), Mats.Emissive(new Color(0.1f, 0.6f, 0.2f), new Color(0.2f, 1.6f, 0.4f)), false, "placa");
            var txt = new GameObject("texto").AddComponent<TextMesh>();
            txt.transform.SetParent(h, false);
            txt.transform.localPosition = new Vector3(0, 3.0f, 0.14f);
            txt.transform.localRotation = Quaternion.Euler(0, 180, 0);
            txt.text = Loc.IsPt ? "SAÍDA" : "EXIT"; txt.characterSize = 0.04f; txt.fontSize = 64;
            txt.anchor = TextAnchor.MiddleCenter; txt.color = new Color(0.9f, 1f, 0.9f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) { txt.font = font; txt.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            var lg = new GameObject("luz_saida"); lg.transform.SetParent(h, false); lg.transform.localPosition = new Vector3(0, 2.6f, 0.8f);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.intensity = 1f; l.color = new Color(0.3f, 1f, 0.45f);
        }

        void AddPickup(int x, int r, HashSet<string> flags, ItemId item, int count, int weapon = -1, int loaded = 0, int doc = -1)
        {
            string id = "p" + x + "_" + r;
            if (flags.Contains(id)) return;
            var go = new GameObject("item_" + id);
            go.transform.SetParent(dynamics, false);
            go.transform.SetPositionAndRotation(CellPos(x, r), Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0));
            var pk = go.AddComponent<Pickup>();
            pk.Id = id; pk.Item = item; pk.Count = count; pk.Weapon = weapon; pk.Loaded = loaded; pk.Doc = doc; pk.Room = RoomOf(x, r);
            BuildPickupVisual(go.transform, pk);
            Pickups.Add(pk);
        }

        void BuildPickupVisual(Transform t, Pickup pk)
        {
            if (pk.Doc >= 0)
            {
                Mats.Part(t, PrimitiveType.Cube, new Vector3(0, 0.01f, 0), new Vector3(0.3f, 0.01f, 0.4f), paperM, "papel");
                Mats.Part(t, PrimitiveType.Cube, new Vector3(0.05f, 0.02f, 0.03f), new Vector3(0.28f, 0.01f, 0.38f), Mats.Lit(new Color(0.78f, 0.74f, 0.62f), 0.05f), "papel", new Vector3(0, 12, 0));
                return;
            }
            if (pk.Weapon == (int)WeaponId.Shotgun)
            {
                var wood = Mats.Lit(new Color(0.3f, 0.18f, 0.1f), 0.3f, 0f, Mats.Wood());
                Mats.Part(t, PrimitiveType.Cylinder, new Vector3(0.15f, 0.05f, 0), new Vector3(0.04f, 0.35f, 0.04f), darkM, "cano", new Vector3(0, 0, 90));
                Mats.Part(t, PrimitiveType.Cube, new Vector3(-0.3f, 0.05f, 0), new Vector3(0.35f, 0.08f, 0.06f), wood, "coronha");
                return;
            }
            if (pk.Weapon == (int)WeaponId.Magnum)
            {
                var steel = Mats.Lit(new Color(0.65f, 0.67f, 0.7f), 0.7f, 0.9f);
                Mats.Part(t, PrimitiveType.Cylinder, new Vector3(0.08f, 0.04f, 0), new Vector3(0.03f, 0.1f, 0.03f), steel, "cano", new Vector3(0, 0, 90));
                Mats.Part(t, PrimitiveType.Cylinder, new Vector3(-0.04f, 0.04f, 0), new Vector3(0.07f, 0.03f, 0.07f), steel, "tambor", new Vector3(0, 0, 90));
                Mats.Part(t, PrimitiveType.Cube, new Vector3(-0.12f, 0.03f, 0), new Vector3(0.1f, 0.05f, 0.04f), darkWoodM, "cabo");
                return;
            }
            var info = ItemDB.Get(pk.Item);
            switch (pk.Item)
            {
                case ItemId.HerbG: case ItemId.HerbR:
                    if (ModelLibrary.Spawn("Erva", t, Vector3.zero, 0.55f) is GameObject herb)
                    {
                        foreach (var rr in herb.GetComponentsInChildren<Renderer>())
                            if (rr.sharedMaterial != null && rr.sharedMaterial.name.ToLower().Contains("folha"))
                                rr.sharedMaterial = Mats.Lit(info.Color, 0.3f);
                        return;
                    }
                    Mats.Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), new Vector3(0.22f, 0.1f, 0.22f), Mats.Lit(new Color(0.45f, 0.25f, 0.15f), 0.2f), "vaso");
                    for (int i = 0; i < 4; i++)
                        Mats.Part(t, PrimitiveType.Sphere, new Vector3(Mathf.Sin(i * 1.6f) * 0.07f, 0.28f + i * 0.03f, Mathf.Cos(i * 1.6f) * 0.07f), new Vector3(0.14f, 0.07f, 0.1f), Mats.Lit(info.Color, 0.3f), "folha", new Vector3(20, i * 90, 0));
                    break;
                case ItemId.Spray:
                    Mats.Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(0.08f, 0.12f, 0.08f), Mats.Lit(new Color(0.9f, 0.9f, 0.92f), 0.5f), "lata");
                    Mats.Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.14f, 0), new Vector3(0.085f, 0.03f, 0.085f), Mats.Lit(new Color(0.2f, 0.7f, 0.3f), 0.4f), "faixa");
                    break;
                case ItemId.AmmoP: case ItemId.AmmoS: case ItemId.AmmoM:
                    Mats.Part(t, PrimitiveType.Cube, new Vector3(0, 0.06f, 0), new Vector3(0.26f, 0.12f, 0.18f), Mats.Lit(info.Color * 0.8f, 0.3f), "caixa_municao");
                    Mats.Part(t, PrimitiveType.Cube, new Vector3(0, 0.07f, -0.091f), new Vector3(0.18f, 0.06f, 0.002f), Mats.Lit(new Color(0.9f, 0.88f, 0.8f), 0.1f), "rotulo");
                    break;
                case ItemId.KeyCrow: case ItemId.KeyLion:
                    var km = Mats.Lit(info.Color, 0.8f, 0.9f);
                    Mats.Part(t, PrimitiveType.Cylinder, new Vector3(-0.08f, 0.02f, 0), new Vector3(0.08f, 0.01f, 0.08f), km, "argola");
                    Mats.Part(t, PrimitiveType.Cube, new Vector3(0.04f, 0.02f, 0), new Vector3(0.16f, 0.015f, 0.02f), km, "haste");
                    Mats.Part(t, PrimitiveType.Cube, new Vector3(0.1f, 0.02f, 0.02f), new Vector3(0.02f, 0.015f, 0.04f), km, "dente");
                    break;
                default: // medalhões em um pequeno suporte
                    Mats.Part(t, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(0.3f, 0.5f, 0.3f), darkWoodM, "suporte");
                    Mats.Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.62f, 0), new Vector3(0.2f, 0.02f, 0.2f), Mats.Lit(info.Color, 0.85f, 1f), "medalhao", new Vector3(80, 0, 0));
                    break;
            }
        }

        void BuildDecor()
        {
            // viatura policial na garagem
            var h = Holder("viatura", CellPos(23, 21) + new Vector3(0, 0, 0.5f), 90f);
            var body = Mats.Lit(new Color(0.9f, 0.9f, 0.88f), 0.6f, 0.3f);
            var black = Mats.Lit(new Color(0.05f, 0.05f, 0.06f), 0.6f, 0.3f);
            Block(h, new Vector3(0, 0.6f, 0), new Vector3(1.8f, 0.6f, 4.4f), body, false, "carroceria");
            Block(h, new Vector3(0, 0.6f, 0), new Vector3(1.82f, 0.25f, 1.6f), black, false, "faixa");
            Block(h, new Vector3(0, 1.15f, -0.2f), new Vector3(1.6f, 0.5f, 2.2f), black, false, "cabine");
            Block(h, new Vector3(-0.3f, 1.45f, -0.2f), new Vector3(0.5f, 0.1f, 0.25f), Mats.Emissive(new Color(0.6f, 0.05f, 0.05f), new Color(1.2f, 0.1f, 0.1f)), false, "sirene");
            Block(h, new Vector3(0.3f, 1.45f, -0.2f), new Vector3(0.5f, 0.1f, 0.25f), Mats.Emissive(new Color(0.05f, 0.1f, 0.6f), new Color(0.1f, 0.2f, 1.2f)), false, "sirene");
            foreach (var x in new[] { -0.85f, 0.85f })
                foreach (var z in new[] { -1.4f, 1.4f })
                    Mats.Part(h, PrimitiveType.Cylinder, new Vector3(x, 0.35f, z), new Vector3(0.65f, 0.12f, 0.65f), black, "roda", new Vector3(0, 0, 90));
            Collide(h, new Vector3(0, 0.75f, 0), new Vector3(1.9f, 1.5f, 4.5f));
        }

        void ScatterBlood()
        {
            int placed = 0, tries = 0;
            while (placed < 45 && tries < 600)
            {
                tries++;
                int x = rnd.Next(1, W - 1), r = rnd.Next(1, H - 1);
                if (G[x, r] == '#' || RoomOf(x, r) == 7) continue;
                Vector3 p = CellPos(x, r) + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) * Cell * 0.8f;
                Fx.Decal(p, Vector3.up, 0.8f + (float)rnd.NextDouble() * 1.6f, Mats.Splat(rnd.Next(8)), dynamics, 0.9f);
                // mancha na parede vizinha
                if (placed % 3 == 0)
                {
                    int[] dx = { 1, -1, 0, 0 }, dr = { 0, 0, 1, -1 };
                    int k = rnd.Next(4);
                    if (G[x + dx[k], r + dr[k]] == '#')
                    {
                        Vector3 n = new Vector3(-dx[k], 0, dr[k]);
                        Vector3 wp = CellPos(x, r) - n * Cell * 0.5f + Vector3.up * (0.8f + (float)rnd.NextDouble() * 1.2f);
                        Fx.Decal(wp, n, 0.7f + (float)rnd.NextDouble(), Mats.Splat(rnd.Next(8)), dynamics, 0.85f);
                    }
                }
                placed++;
            }
        }

        static void ApplyEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.06f, 0.065f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.012f, 0.014f, 0.014f);
            RenderSettings.fogDensity = 0.045f;
        }
    }
}
