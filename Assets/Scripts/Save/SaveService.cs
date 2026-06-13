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

        public AccountSaveData Load()
        {
            if (!HasSave())
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var accountData = JsonUtility.FromJson<AccountSaveData>(json);
                if (accountData != null && accountData.characters != null && accountData.characters.Count > 0)
                {
                    accountData.EnsureCollections();
                    return accountData;
                }

                var legacySave = JsonUtility.FromJson<PlayerSaveData>(json);
                if (legacySave == null || string.IsNullOrEmpty(legacySave.characterId))
                {
                    return null;
                }

                legacySave.EnsureCollections();
                if (string.IsNullOrEmpty(legacySave.lastSavedUtc))
                {
                    legacySave.lastSavedUtc = DateTime.UtcNow.ToString("O");
                }

                return AccountSaveData.FromLegacy(legacySave);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load save data at {SavePath}: {exception.Message}");
                return null;
            }
        }

        public void Save(AccountSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.EnsureCollections();
            saveData.lastSavedUtc = DateTime.UtcNow.ToString("O");
            var active = saveData.GetActiveCharacter();
            if (active != null)
            {
                active.lastSavedUtc = saveData.lastSavedUtc;
            }

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
