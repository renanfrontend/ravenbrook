using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Ravenbrook.EditorTools
{
    /// <summary>
    /// Menu "Ravenbrook": configura o projeto, gera modelos no Blender e compila o .exe.
    /// Linha de comando:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod Ravenbrook.EditorTools.BuildTools.BuildWindows
    /// </summary>
    public static class BuildTools
    {
        const string ScenePath = "Assets/Scenes/Ravenbrook.unity";

        static readonly string[] RuntimeShaders =
        {
            "Standard", "Sprites/Default", "UI/Default", "Legacy Shaders/Diffuse"
        };

        [MenuItem("Ravenbrook/Configurar projeto", false, 1)]
        public static void Setup()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "Renan";
            PlayerSettings.productName = "Ravenbrook - O Surto";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // entrada antiga (Input Manager), usada pelos scripts
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps.Length > 0)
            {
                var so = new SerializedObject(ps[0]);
                var p = so.FindProperty("activeInputHandler");
                if (p != null && p.intValue != 0) { p.intValue = 0; so.ApplyModifiedProperties(); }
            }

            SetupLayers();
            SetupGraphics();
            AssetDatabase.SaveAssets();
            Debug.Log("[Ravenbrook] Projeto configurado.");
        }

        static void SetupLayers()
        {
            var tm = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tm.Length == 0) return;
            var so = new SerializedObject(tm[0]);
            var layers = so.FindProperty("layers");
            string[] names = { "Jogador", "Inimigo", "Interativo", "Porta" };
            for (int i = 0; i < names.Length; i++)
            {
                var el = layers.GetArrayElementAtIndex(8 + i);
                if (string.IsNullOrEmpty(el.stringValue)) el.stringValue = names[i];
            }
            so.ApplyModifiedProperties();
        }

        static void SetupGraphics()
        {
            var gs = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(gs);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            void Include(Shader sh)
            {
                if (sh == null) return;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sh;
            }
            foreach (var n in RuntimeShaders) Include(Shader.Find(n));
            // os shaders próprios já vão no build por estarem em Resources; incluí-los aqui também quebra o build
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var sh = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (sh != null && !AssetDatabase.GetAssetPath(sh).StartsWith("Assets/")) continue;
                list.DeleteArrayElementAtIndex(i);
                if (i < list.arraySize && list.GetArrayElementAtIndex(i).objectReferenceValue == null) list.DeleteArrayElementAtIndex(i);
            }

            // a cena é vazia: sem isto o build remove as variantes de neblina
            var fog = so.FindProperty("m_FogStripping"); if (fog != null) fog.intValue = 1;
            var lin = so.FindProperty("m_FogKeepLinear"); if (lin != null) lin.boolValue = true;
            var e1 = so.FindProperty("m_FogKeepExp"); if (e1 != null) e1.boolValue = true;
            var e2 = so.FindProperty("m_FogKeepExp2"); if (e2 != null) e2.boolValue = true;
            so.ApplyModifiedProperties();
        }

        static string BuildFolder()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-pastaBuild");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : "Build";
        }

        [MenuItem("Ravenbrook/Gerar jogo para Windows", false, 20)]
        public static void BuildWindows()
        {
            Setup();
            string folder = BuildFolder();
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = folder + "/Ravenbrook.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            var s = report.summary;
            if (s.result == BuildResult.Succeeded && File.Exists("Assets/Resources/Modelos/CREDITOS.txt"))
                File.Copy("Assets/Resources/Modelos/CREDITOS.txt", Path.Combine(folder, "CREDITOS-MODELOS.txt"), true);
            Debug.Log($"[Ravenbrook] Build: {s.result} | {s.totalSize / (1024 * 1024)} MB | {s.totalTime}");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        // ---------------- Blender ----------------

        static string FindBlender()
        {
            string env = System.Environment.GetEnvironmentVariable("BLENDER_EXE");
            if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;
            string root = @"C:\Program Files\Blender Foundation";
            if (!Directory.Exists(root)) return null;
            string best = null;
            foreach (var d in Directory.GetDirectories(root))
            {
                string exe = Path.Combine(d, "blender.exe");
                if (File.Exists(exe) && (best == null || string.CompareOrdinal(d, Path.GetDirectoryName(best)) > 0)) best = exe;
            }
            return best;
        }

        [MenuItem("Ravenbrook/Gerar modelos no Blender", false, 40)]
        public static void GenerateBlenderModels()
        {
            string blender = FindBlender();
            if (blender == null) { Debug.LogError("[Ravenbrook] Blender não encontrado. Defina a variável BLENDER_EXE."); return; }
            string script = Path.GetFullPath("Blender/gerar_modelos.py");
            string outDir = Path.GetFullPath("Assets/Resources/Modelos");
            var psi = new ProcessStartInfo(blender, $"-b --factory-startup -P \"{script}\" -- \"{outDir}\"")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using (var proc = Process.Start(psi))
            {
                string output = proc.StandardOutput.ReadToEnd();
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                foreach (var line in output.Split('\n')) if (line.Contains("[Ravenbrook]")) Debug.Log(line.Trim());
                if (proc.ExitCode != 0) Debug.LogError("[Ravenbrook] Blender terminou com erro " + proc.ExitCode + "\n" + err);
            }
            AssetDatabase.Refresh();
            Debug.Log("[Ravenbrook] Modelos do Blender importados em Assets/Resources/Modelos.");
        }

        [MenuItem("Ravenbrook/Abrir guia do Sketchfab", false, 41)]
        public static void OpenSketchfabGuide() => EditorUtility.OpenWithDefaultApp(Path.GetFullPath("TUTORIAL-SKETCHFAB.md"));

        /// <summary>Confere como os modelos foram importados (escala e eixos).</summary>
        public static void InspectModels()
        {
            foreach (var name in new[] { "Jogador", "Zumbi", "Brutamonte", "Caixa", "Mesa", "Estatua" })
            {
                var g = Resources.Load<GameObject>("Modelos/" + name);
                if (g == null) { Debug.Log($"[Ravenbrook] {name}: não existe"); continue; }
                var inst = Object.Instantiate(g);
                var b = new Bounds(inst.transform.position, Vector3.zero);
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                string parts = "";
                foreach (Transform t in inst.GetComponentsInChildren<Transform>())
                    if (t.name == "Tronco" || t.name == "BracoD" || t.name == "Cabeca")
                        parts += $" {t.name}@{t.position:F2} rot{t.rotation.eulerAngles:F0}";
                Debug.Log($"[Ravenbrook] {name}: raiz rot {inst.transform.rotation.eulerAngles:F0} · limites {b.min:F2}..{b.max:F2}{parts}");
                Object.DestroyImmediate(inst);
            }
        }
    }

    /// <summary>Importa modelos de Resources/Modelos com os eixos já convertidos (Blender Z-up → Unity Y-up).</summary>
    public class ModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Modelos/")) return;
            var mi = (ModelImporter)assetImporter;
            mi.bakeAxisConversion = true;
            mi.useFileScale = true;
            mi.importCameras = false; mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }
    }
}
