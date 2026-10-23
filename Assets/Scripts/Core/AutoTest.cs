using System.Collections;
using System.IO;
using UnityEngine;

namespace Ravenbrook
{
    /// <summary>
    /// Teste automático: Ravenbrook.exe -autotest
    /// Joga sozinho, tira prints (Build/autotest_*.png) e registra FPS e erros no log com o prefixo [AutoTest].
    /// </summary>
    public class AutoTest : MonoBehaviour
    {
        int errors, shot;
        float fpsSum; int fpsCount; float fpsMin = 999;
        string dir;

        void Start()
        {
            dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { errors++; Debug.Log("[AutoTest] ERRO capturado: " + msg); }
            };
            StartCoroutine(Run());
        }

        void Update()
        {
            if (Time.unscaledDeltaTime <= 0) return;
            float f = 1f / Time.unscaledDeltaTime;
            if (Time.unscaledTime > 3f) { fpsSum += f; fpsCount++; fpsMin = Mathf.Min(fpsMin, f); }
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(dir, $"autotest_{shot++:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[AutoTest] print " + path);
            yield return new WaitForSecondsRealtime(0.4f);
        }

        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        IEnumerator Run()
        {
            var gm = GameManager.I;
            bool fpsOnly = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-fpsonly") >= 0;
            yield return Wait(2.5f);
            Debug.Log($"[AutoTest] início · tela {Screen.width}x{Screen.height} · qualidade {Settings.Quality} · GPU {SystemInfo.graphicsDeviceName}");
            if (!fpsOnly)
            {
                yield return Shot("titulo");
                int lang = Settings.Language;
                gm.SetLanguage(10); yield return Wait(0.5f); yield return Shot("titulo_japones");
                gm.SetLanguage(lang); yield return Wait(0.3f);
            }

            gm.NewGame();
            var p = gm.Player;
            yield return Wait(2f);
            yield return Shot("saguao");

            // anda para a frente e para trás
            p.SimMove = new Vector2(0, 1); yield return Wait(1.2f);
            p.SimMove = Vector2.zero; yield return Wait(0.3f);
            Debug.Log("[AutoTest] andou até " + p.transform.position);

            // combate no escritório leste
            gm.UI.Teleport(5);
            yield return Wait(0.5f);
            Zombie target = null; float best = 999;
            foreach (var z in Zombie.All)
                if (!z.Dead && z.Type != ZType.Brute && gm.Level.RoomAt(z.transform.position) == 5)
                {
                    float d = Vector3.Distance(z.transform.position, p.transform.position);
                    if (d < best) { best = d; target = z; }
                }
            if (target != null)
            {
                Vector3 to = target.transform.position + Vector3.up * 1.5f - gm.Cam.transform.position;
                var cam = ThirdPersonCamera.I;
                cam.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                cam.Pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
                p.SimAim = true;
                yield return Wait(1.2f);
                yield return Shot("mirando");
                int kills = gm.Kills;
                for (int i = 0; i < 8 && !target.Dead; i++)
                {
                    to = target.transform.position + Vector3.up * 1.3f - gm.Cam.transform.position;
                    cam.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    cam.Pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
                    yield return null;
                    var ct = gm.Cam.transform;
                    string hitName = Physics.Raycast(ct.position, ct.forward, out var dbg, 60f, Layers.ShootMask, QueryTriggerInteraction.Ignore)
                        ? $"{dbg.collider.name} (camada {dbg.collider.gameObject.layer}) a {dbg.distance:0.0} m" : "nada";
                    Debug.Log($"[AutoTest] tiro {i}: alvo {target.transform.position} cam {ct.position} → acerta {hitName}");
                    p.Weapons.Fire();
                    yield return Wait(0.45f);
                }
                Debug.Log($"[AutoTest] combate: alvo morto={target.Dead} abates={gm.Kills - kills} munição={p.Weapons.Mag[1]} vida={p.Hp}");
                yield return Shot("combate");
                p.SimAim = false;
            }
            else Debug.Log("[AutoTest] nenhum zumbi no escritório leste");

            if (!fpsOnly)
            {
                gm.SetState(GameState.Inventory); yield return Wait(0.4f); yield return Shot("inventario");
                gm.SetState(GameState.Map); yield return Wait(0.4f); yield return Shot("mapa");
                gm.SetState(GameState.Playing);
                gm.ToggleOverwatch(); yield return Wait(0.4f); yield return Shot("overwatch");
                Cheats.God = true; Cheats.Telemetry = true;
                gm.ToggleOverwatch();
            }
            else { Cheats.God = true; }

            foreach (int room in new[] { 1, 0, 6, 7, 8, 2 })
            {
                gm.UI.Teleport(room);
                ThirdPersonCamera.I.Pitch = 8;
                yield return Wait(1.5f);
                if (!fpsOnly) yield return Shot("sala_" + room);
            }

            gm.UI.Teleport(4);
            gm.SpawnBrute();
            yield return Wait(3f);
            if (!fpsOnly) yield return Shot("perseguidor");

            // salvar e carregar
            gm.UI.Teleport(7);
            yield return Wait(0.5f);
            gm.SaveGame();
            Vector3 saved = p.transform.position;
            yield return Wait(0.5f);
            bool ok = gm.ContinueGame();
            Debug.Log($"[AutoTest] salvar/carregar: {ok} · posição {saved} → {gm.Player.transform.position}");
            p = gm.Player;

            // documento
            foreach (var it in Interactable.All)
                if (it is Pickup pk && pk.Doc >= 0) { pk.Interact(p); break; }
            yield return Wait(0.5f);
            if (!fpsOnly) yield return Shot("documento");
            gm.SetState(GameState.Playing);

            // estátua → portão → vitória
            p.Inv.Add(ItemId.MedalSun, 1); p.Inv.Add(ItemId.MedalMoon, 1); p.Inv.Add(ItemId.MedalStar, 1);
            gm.UseStatue(p);
            yield return Wait(3f);
            Debug.Log("[AutoTest] portão aberto: " + gm.Flags.Contains("gate"));
            p.Teleport(gm.Level.ExitPos + Vector3.forward * 0.5f);
            yield return Wait(1.5f);
            if (!fpsOnly) yield return Shot("vitoria");

            Debug.Log($"[AutoTest] FIM · FPS médio {(fpsCount > 0 ? fpsSum / fpsCount : 0):0.0} · mínimo {fpsMin:0.0} · erros {errors} · estado {gm.State}");
            yield return Wait(0.5f);
            Application.Quit();
        }
    }
}
