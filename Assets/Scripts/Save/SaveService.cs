using System;
using System.IO;
using UnityEngine;

namespace IdleOnLike.Save
{
    public sealed class SaveService
    {
        private const string SaveFileName = "idleon_like_save.json";

        public string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public PlayerSaveData Load()
        {
            if (!HasSave())
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var saveData = JsonUtility.FromJson<PlayerSaveData>(json);
                saveData?.EnsureCollections();
                if (saveData != null && string.IsNullOrEmpty(saveData.lastSavedUtc))
                {
                    saveData.lastSavedUtc = DateTime.UtcNow.ToString("O");
                }
                return saveData;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load save data at {SavePath}: {exception.Message}");
                return null;
            }
        }

        public void Save(PlayerSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.EnsureCollections();
            saveData.lastSavedUtc = DateTime.UtcNow.ToString("O");
            var json = JsonUtility.ToJson(saveData, true);
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, json);
        }

        public void DeleteSave()
        {
            if (HasSave())
            {
                File.Delete(SavePath);
            }
        }
    }
}
