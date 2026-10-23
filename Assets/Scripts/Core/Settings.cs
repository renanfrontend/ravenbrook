using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ravenbrook
{
    /// <summary>Preferências do jogador (PlayerPrefs).</summary>
    public static class Settings
    {
        public static int Language = 1;
        public static float Sensitivity = 2.5f;
        public static float Volume = 0.8f;
        public static int Quality = 1;          // 0 Baixo, 1 Médio, 2 Alto
        public static bool DevMode = true;      // libera o Painel Overwatch (F1)

        public static bool PostFX => Quality >= 1;

        public static void Load()
        {
            Language = PlayerPrefs.GetInt("rb_lang", DefaultLanguage());
            Sensitivity = PlayerPrefs.GetFloat("rb_sens", 2.5f);
            Volume = PlayerPrefs.GetFloat("rb_vol", 0.8f);
            Quality = PlayerPrefs.GetInt("rb_quality", 1);
            DevMode = PlayerPrefs.GetInt("rb_dev", 1) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("rb_lang", Language);
            PlayerPrefs.SetFloat("rb_sens", Sensitivity);
            PlayerPrefs.SetFloat("rb_vol", Volume);
            PlayerPrefs.SetInt("rb_quality", Quality);
            PlayerPrefs.SetInt("rb_dev", DevMode ? 1 : 0);
            PlayerPrefs.Save();
        }

        static int DefaultLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Portuguese: return 0;
                case SystemLanguage.Spanish: return 2;
                case SystemLanguage.French: return 3;
                case SystemLanguage.German: return 4;
                case SystemLanguage.Italian: return 5;
                case SystemLanguage.Russian: return 6;
                case SystemLanguage.Polish: return 7;
                case SystemLanguage.Turkish: return 8;
                case SystemLanguage.Dutch: return 9;
                case SystemLanguage.Japanese: return 10;
                case SystemLanguage.Korean: return 11;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional: return 12;
                default: return 1;
            }
        }

        public static void ApplyQuality()
        {
            AudioListener.volume = Volume;
            QualitySettings.vSyncCount = 0;
            switch (Quality)
            {
                case 0:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.pixelLightCount = 2;
                    QualitySettings.antiAliasing = 0;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.pixelLightCount = 4;
                    QualitySettings.antiAliasing = 0;
                    break;
                default:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.pixelLightCount = 6;
                    QualitySettings.antiAliasing = 4;
                    break;
            }
            QualitySettings.shadowDistance = 30f;
            if (GameManager.I != null && GameManager.I.Player != null) GameManager.I.Player.ApplyQuality();
        }
    }

    /// <summary>Atalhos do Painel Overwatch.</summary>
    public static class Cheats
    {
        public static bool God, InfiniteAmmo, UnbreakableKnife, Telemetry, FreezeEnemies;
        public static void Reset() { God = InfiniteAmmo = UnbreakableKnife = Telemetry = FreezeEnemies = false; }
    }

    [System.Serializable]
    public class SaveData
    {
        public int version = 1;
        public float time; public int kills, saves; public bool cheats;
        public float hp, x, y, z, yaw;
        public int[] invIds, invCounts, boxIds, boxCounts;
        public bool[] owned; public int[] mags; public float knife; public int weapon;
        public List<string> flags = new List<string>();
        public List<int> docs = new List<int>();
    }

    /// <summary>Salvamento na máquina de escrever (JSON na pasta de dados do jogo).</summary>
    public static class SaveSystem
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "ravenbrook_save.json");
        public static bool Exists => File.Exists(FilePath);

        public static void Save(SaveData d)
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(d, true)); }
            catch (System.Exception e) { Debug.LogError("Falha ao salvar: " + e.Message); }
        }

        public static SaveData Load()
        {
            try { return Exists ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) : null; }
            catch (System.Exception e) { Debug.LogError("Falha ao carregar: " + e.Message); return null; }
        }
    }
}
