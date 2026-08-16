using System;
using System.IO;
using UnityEngine;

namespace TapLegends.Core
{
    /// <summary>
    /// Persistencia en Application.persistentDataPath como JSON. Escribe a un archivo
    /// temporal y lo renombra al final: un cierre a mitad de escritura no corrompe el
    /// guardado real (ver TapLegends-Plan.md, Etapa 4).
    /// </summary>
    public static class SaveSystem
    {
        private const int CurrentVersion = 1;
        private const string FileName = "save.json";
        private const string TempFileName = "save.tmp";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => Path.Combine(Application.persistentDataPath, TempFileName);

        public static void Save(SaveData data)
        {
            data.saveVersion = CurrentVersion;
            data.lastSaveTimestampUtc = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data);

            try
            {
                File.WriteAllText(TempPath, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(TempPath, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] No se pudo guardar: {e.Message}");
            }
        }

        /// <summary>Null si no hay guardado o si esta corrupto (partida nueva en ese caso).</summary>
        public static SaveData Load()
        {
            if (!File.Exists(SavePath)) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                return Migrate(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Guardado ilegible, se ignora: {e.Message}");
                return null;
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }

        /// <summary>Punto unico de migracion: si CurrentVersion sube, los pasos van aca en cadena.</summary>
        private static SaveData Migrate(SaveData data)
        {
            if (data == null) return null;
            // v1 es la unica version por ahora. Ejemplo para el futuro:
            // if (data.saveVersion < 2) { ...ajustar campos...; data.saveVersion = 2; }
            return data;
        }
    }
}
