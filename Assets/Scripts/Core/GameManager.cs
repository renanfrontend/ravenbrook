using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ravenbrook
{
    public enum GameState { Title, Playing, Paused, Inventory, Map, Document, ItemBox, GameOver, Victory }

    public static class Layers
    {
        public const int Default = 0, Player = 8, Enemy = 9, Interact = 10, Door = 11;
        public static readonly int ShootMask = (1 << Default) | (1 << Enemy) | (1 << Door);
        public static readonly int SolidMask = (1 << Default) | (1 << Door);
    }

    /// <summary>
    /// Coração do jogo. Monta-se sozinho ao abrir a cena (RuntimeInitializeOnLoadMethod):
    /// câmera, sons, interface, delegacia e o fluxo título → jogo → morte/vitória.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;
        public GameState State { get; private set; } = GameState.Title;
        public PlayerController Player { get; private set; }
        public LevelBuilder Level { get; private set; }
        public UIRoot UI { get; private set; }
        public Camera Cam { get; private set; }

        public readonly HashSet<string> Flags = new HashSet<string>();
        public readonly List<int> DocsRead = new List<int>();
        public float PlayTime; public int Kills, Saves; public bool CheatsUsed;
        public float GameSpeed = 1f;
        public bool OverwatchOpen;
        public bool AutoTestMode;
        public string LastObjective = "";

        float nextVisit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        void Awake()
        {
            I = this;
            DontDestroyOnLoad(gameObject);
            var args = System.Environment.GetCommandLineArgs();
            AutoTestMode = System.Array.IndexOf(args, "-autotest") >= 0;
            Settings.Load();
            Loc.Lang = Settings.Language;
            Application.targetFrameRate = 60;
            Settings.ApplyQuality();
            Sfx.Init(gameObject);

            foreach (var c in FindObjectsByType<Camera>()) Destroy(c.gameObject);
            var camGo = new GameObject("CameraPrincipal");
            camGo.tag = "MainCamera";
            Cam = camGo.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Color.black;
            Cam.nearClipPlane = 0.05f; Cam.farClipPlane = 80f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PostFX>();
            camGo.AddComponent<ThirdPersonCamera>();
            DontDestroyOnLoad(camGo);

            UI = UIRoot.Create();
            BuildWorld(null, false);
            SetState(GameState.Title);
            if (AutoTestMode) gameObject.AddComponent<AutoTest>();
        }

        // ---------------- mundo ----------------

        void BuildWorld(SaveData data, bool spawnPlayer)
        {
            if (Player != null) { DestroyImmediate(Player.gameObject); Player = null; }
            if (Level != null) DestroyImmediate(Level.gameObject);
            Fx.ClearDecals();
            Level = LevelBuilder.Build(Flags);
            ThirdPersonCamera.I.TitleCenter = Level.CellPos(15, 12);
            if (!spawnPlayer) { ThirdPersonCamera.I.Target = null; return; }

            Vector3 pos = data != null ? new Vector3(data.x, data.y, data.z) : Level.PlayerStart;
            float yaw = data != null ? data.yaw : Level.StartYaw;
            Player = PlayerController.Spawn(pos, yaw, Level.transform);
            if (data != null)
            {
                Player.Hp = data.hp;
                Inventory.Read(Player.Inv.Slots, data.invIds, data.invCounts);
                Inventory.Read(Player.Inv.Box, data.boxIds, data.boxCounts);
                if (data.owned != null) for (int i = 0; i < 4 && i < data.owned.Length; i++) Player.Weapons.Owned[i] = data.owned[i];
                if (data.mags != null) for (int i = 0; i < 4 && i < data.mags.Length; i++) Player.Weapons.Mag[i] = data.mags[i];
                Player.Weapons.KnifeDur = data.knife;
                Player.Weapons.Select(data.weapon);
            }
            else
            {
                Player.Inv.Add(ItemId.AmmoP, 18);
                Player.Inv.Add(ItemId.HerbG, 1);
            }
            ThirdPersonCamera.I.SnapBehind(Player.transform);
        }

        public void NewGame()
        {
            Flags.Clear(); DocsRead.Clear();
            PlayTime = 0; Kills = 0; Saves = 0; CheatsUsed = false;
            Cheats.Reset(); GameSpeed = 1f;
            BuildWorld(null, true);
            SetState(GameState.Playing);
            UI.ShowControlsHint();
        }

        public bool ContinueGame()
        {
            var d = SaveSystem.Load();
            if (d == null) return false;
            Flags.Clear(); foreach (var f in d.flags) Flags.Add(f);
            DocsRead.Clear(); DocsRead.AddRange(d.docs);
            PlayTime = d.time; Kills = d.kills; Saves = d.saves; CheatsUsed = d.cheats;
            Cheats.Reset(); GameSpeed = 1f;
            BuildWorld(d, true);
            SetState(GameState.Playing);
            return true;
        }

        public void GoTitle()
        {
            Flags.Clear();
            Cheats.Reset(); GameSpeed = 1f; OverwatchOpen = false;
            BuildWorld(null, false);
            SetState(GameState.Title);
        }

        // ---------------- estados ----------------

        public void SetState(GameState s)
        {
            State = s;
            if (s != GameState.Playing) OverwatchOpen = false;
            ApplyTime();
            UI.OnState(s);
        }

        public void ApplyTime()
        {
            bool paused = State == GameState.Paused || State == GameState.Inventory || State == GameState.Map ||
                          State == GameState.Document || State == GameState.ItemBox;
            Time.timeScale = paused ? 0f : State == GameState.Playing ? GameSpeed : 1f;
            bool locked = State == GameState.Playing && !OverwatchOpen;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void ToggleOverwatch()
        {
            if (State != GameState.Playing || !Settings.DevMode) return;
            OverwatchOpen = !OverwatchOpen;
            ApplyTime();
            UI.ShowOverwatch(OverwatchOpen);
        }

        void Update()
        {
            if (State == GameState.Playing) PlayTime += Time.deltaTime;

            if (!AutoTestMode)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (OverwatchOpen) ToggleOverwatch();
                    else if (State == GameState.Playing) SetState(GameState.Paused);
                    else if (State == GameState.Paused || State == GameState.Inventory || State == GameState.Map ||
                             State == GameState.Document || State == GameState.ItemBox) SetState(GameState.Playing);
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (State == GameState.Playing && !OverwatchOpen) SetState(GameState.Inventory);
                    else if (State == GameState.Inventory) SetState(GameState.Playing);
                }
                if (Input.GetKeyDown(KeyCode.M))
                {
                    if (State == GameState.Playing && !OverwatchOpen) SetState(GameState.Map);
                    else if (State == GameState.Map) SetState(GameState.Playing);
                }
                if (Input.GetKeyDown(KeyCode.F1)) ToggleOverwatch();
            }

            if (State == GameState.Playing && Player != null && !Player.Dead)
            {
                if (Time.time > nextVisit)
                {
                    nextVisit = Time.time + 0.5f;
                    int room = Level.RoomAt(Player.transform.position);
                    if (room >= 0) Flags.Add("v" + room);
                    Zombie.CountChasing();
                    Sfx.Tension = Mathf.Clamp01(Zombie.Chasing / 4f);
                    string obj = ObjectiveText();
                    if (obj != LastObjective) { LastObjective = obj; UI.ShowObjective(); }
                }
                if (Flags.Contains("gate") && Vector3.Distance(Player.transform.position, Level.ExitPos) < 1.8f) Victory();
            }
            else Sfx.Tension = 0;
        }

        // ---------------- eventos do jogo ----------------

        public void Toast(string msg) => UI.Toast(msg);

        public void ReadDocument(int id)
        {
            if (!DocsRead.Contains(id)) DocsRead.Add(id);
            Sfx.Play("pickup", 0.5f);
            UI.ShowDocument(id);
            SetState(GameState.Document);
        }

        public void OpenItemBox()
        {
            Sfx.Play("ui");
            SetState(GameState.ItemBox);
        }

        public void SaveGame()
        {
            Saves++;
            var p = Player;
            var d = new SaveData
            {
                time = PlayTime, kills = Kills, saves = Saves, cheats = CheatsUsed,
                hp = p.Hp, x = p.transform.position.x, y = p.transform.position.y, z = p.transform.position.z, yaw = p.transform.eulerAngles.y,
                owned = (bool[])p.Weapons.Owned.Clone(), mags = (int[])p.Weapons.Mag.Clone(), knife = p.Weapons.KnifeDur, weapon = p.Weapons.Current,
                flags = new List<string>(Flags), docs = new List<int>(DocsRead)
            };
            Inventory.Write(p.Inv.Slots, out d.invIds, out d.invCounts);
            Inventory.Write(p.Inv.Box, out d.boxIds, out d.boxCounts);
            SaveSystem.Save(d);
            Sfx.Play("save");
            Toast(Loc.Get("saved"));
        }

        public int MedalsPlaced()
        {
            int n = 0;
            for (int i = 0; i < 3; i++) if (Flags.Contains("medal_" + i)) n++;
            return n;
        }

        public int MedalsFound()
        {
            int n = 0;
            for (int i = 0; i < 3; i++) if (Flags.Contains("found_" + i)) n++;
            return n;
        }

        public void OnMedalFound(ItemId medal)
        {
            Flags.Add("found_" + ((int)medal - (int)ItemId.MedalSun));
            if (MedalsFound() >= 2 && !Flags.Contains("brute")) SpawnBrute();
        }

        public void SpawnBrute()
        {
            if (Level == null) return;
            Flags.Add("brute");
            Level.SpawnBrute();
            Sfx.Play("sting", 0.9f);
            Sfx.Play("roar", 0.6f);
            Toast(Loc.Get("pursuer"));
        }

        public void UseStatue(PlayerController p)
        {
            Flags.Add("statue_seen");
            bool placedAny = false;
            for (int i = 0; i < 3; i++)
            {
                var medal = (ItemId)((int)ItemId.MedalSun + i);
                if (p.Inv.Has(medal)) { p.Inv.Take(medal, 1); Flags.Add("medal_" + i); placedAny = true; }
            }
            Level.StatueObj.Refresh();
            int placed = MedalsPlaced();
            if (placedAny) { Sfx.Play("pickup"); Toast(Loc.Get("medal_placed")); }
            if (placed >= 3)
            {
                if (!Flags.Contains("gate"))
                {
                    Flags.Add("gate");
                    foreach (var d in Level.Doors) if (d.Gate) d.OpenFrom(p.transform.position);
                    Sfx.Play("sting", 0.5f);
                    Toast(Loc.Get("gate_open"));
                }
            }
            else if (!placedAny) Toast(placed == 0 ? Loc.Get("statue_hint") : Loc.F("statue_need", 3 - placed));
            else Toast(Loc.F("statue_need", 3 - placed));
        }

        public void OnZombieKilled(Zombie z)
        {
            Kills++;
            Flags.Add(z.Id);
        }

        public void OnPlayerDied() => StartCoroutine(DeathRoutine());

        IEnumerator DeathRoutine()
        {
            Sfx.Play("sting", 0.7f);
            yield return new WaitForSecondsRealtime(2.2f);
            if (Player != null && Player.Dead) SetState(GameState.GameOver);
        }

        public void Victory()
        {
            if (State == GameState.Victory) return;
            Flags.Add("gate_used");
            Sfx.Play("gate", 0.6f);
            SetState(GameState.Victory);
        }

        public string Rank()
        {
            if (CheatsUsed) return "—";
            float min = PlayTime / 60f;
            if (min < 20 && Saves <= 3) return "S";
            if (min < 35) return "A";
            if (min < 60) return "B";
            return "C";
        }

        public string ObjectiveText()
        {
            if (Flags.Contains("gate")) return Loc.Get("obj_escape");
            if (MedalsFound() > 0 || Flags.Contains("statue_seen")) return Loc.F("obj_medals", Mathf.Max(MedalsFound(), MedalsPlaced()));
            return Loc.Get("obj_explore");
        }

        public void MarkCheat() { CheatsUsed = true; }

        public void SetLanguage(int lang)
        {
            Settings.Language = (lang + Loc.Names.Length) % Loc.Names.Length;
            Loc.Lang = Settings.Language;
            Settings.Save();
            UI.Rebuild();
            LastObjective = "";
        }

        public static string FormatTime(float t)
        {
            int s = Mathf.FloorToInt(t);
            return $"{s / 3600:00}:{s / 60 % 60:00}:{s % 60:00}";
        }
    }
}
