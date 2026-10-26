using System;
using System.IO;
using StellarIdle.Core;
using UnityEngine;

namespace StellarIdle
{
    /// <summary>
    /// JSON save in persistentDataPath, written through a temp file with a backup of the previous save,
    /// so a crash or a full disk mid-write never loses the player's progress.
    /// </summary>
    public sealed class SaveService
    {
        readonly string path;
        readonly string backupPath;
        readonly string tempPath;

        public SaveService(string fileName = "save.json")
        {
            path = Path.Combine(Application.persistentDataPath, fileName);
            backupPath = path + ".bak";
            tempPath = path + ".tmp";
        }

        public GameState Load() => TryLoad(path) ?? TryLoad(backupPath);

        public void Save(GameState state)
        {
            try
            {
                File.WriteAllText(tempPath, JsonUtility.ToJson(state));
                if (File.Exists(path)) File.Copy(path, backupPath, true);
                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to write save: {e}");
            }
        }

        public void Delete()
        {
            foreach (var p in new[] { path, backupPath, tempPath })
                if (File.Exists(p)) File.Delete(p);
        }

        static GameState TryLoad(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                string json = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<GameState>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Could not read '{file}': {e.Message}");
                return null;
            }
        }
    }
}
