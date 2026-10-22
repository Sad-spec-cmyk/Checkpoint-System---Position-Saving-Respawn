using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CheckpointSystem
{
    /// <summary>Local JSON storage with per-slot/per-scene keys, validation and backup recovery.</summary>
    public sealed class JsonCheckpointStore : ICheckpointStore
    {
        private readonly string directory;
        public JsonCheckpointStore(string directory = null)
        {
            this.directory = directory ?? Path.Combine(Application.persistentDataPath, "CheckpointSystem");
        }

        public string GetPath(string slot, string sceneKey)
        {
            // Hash the complete key: no user-controlled path segments, collisions from sanitizing,
            // or delimiter ambiguities. A different scene/slot always has its own save file.
            string key = (slot ?? "").Length + ":" + (slot ?? "") + (sceneKey ?? "");
            using (var hash = SHA256.Create())
                return Path.Combine(directory, BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(key))).Replace("-", "").ToLowerInvariant() + ".json");
        }

        public bool TrySave(string slot, CheckpointSnapshot snapshot, out string error)
        {
            error = null;
            if (!snapshot.IsValid) { error = "The checkpoint pose or schema is invalid."; return false; }
            try
            {
                Directory.CreateDirectory(directory);
                string path = GetPath(slot, snapshot.sceneKey);
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(snapshot, true), Encoding.UTF8);
                if (File.Exists(path))
                {
                    // Keep the last readable save. Never replace a good backup with corrupt data.
                    if (TryRead(path, snapshot.sceneKey, out _, out _)) File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                }
                File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception ex) when (IsStorageError(ex)) { error = ex.Message; return false; }
        }

        public bool TryLoad(string slot, string sceneKey, out CheckpointSnapshot snapshot, out string error)
        {
            string path = GetPath(slot, sceneKey);
            if (TryRead(path, sceneKey, out snapshot, out error)) return true;
            string primaryError = error;
            if (TryRead(path + ".bak", sceneKey, out snapshot, out var backupError))
            { error = "Recovered the previous checkpoint from the backup file."; return true; }
            error = primaryError ?? backupError;
            return false;
        }

        private static bool TryRead(string path, string sceneKey, out CheckpointSnapshot snapshot, out string error)
        {
            snapshot = default;
            error = null;
            if (!File.Exists(path)) return false;
            try
            {
                if (new FileInfo(path).Length > 65536) throw new IOException("Checkpoint file exceeds 64 KB.");
                snapshot = JsonUtility.FromJson<CheckpointSnapshot>(File.ReadAllText(path));
                if (!snapshot.IsValid || snapshot.sceneKey != sceneKey)
                    throw new IOException("Checkpoint file has invalid data, scene scope or schema version.");
                return true;
            }
            catch (Exception ex) when (IsStorageError(ex)) { error = ex.Message; snapshot = default; return false; }
        }

        public bool TryDelete(string slot, string sceneKey, out string error)
        {
            error = null;
            try
            {
                string path = GetPath(slot, sceneKey);
                foreach (string suffix in new[] { "", ".bak", ".tmp" })
                    if (File.Exists(path + suffix)) File.Delete(path + suffix);
                return true;
            }
            catch (Exception ex) when (IsStorageError(ex)) { error = ex.Message; return false; }
        }

        private static bool IsStorageError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException
            || ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException;
    }
}
