using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CheckpointSystem
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Checkpoint System/Checkpoint Manager")]
    public sealed class CheckpointManager : MonoBehaviour
    {
        [SerializeField] private RespawnTarget player;
        [SerializeField] private Transform initialSpawn;
        [Tooltip("Stable level identifier. If empty, the scene path (or scene name) is used.")]
        [SerializeField] private string sceneKey = "";
        [SerializeField] private string saveSlot = "player-01";
        [SerializeField] private bool persistToDisk = true;
        [SerializeField] private bool restoreOnStart = true;
        [SerializeField] private bool requireIncreasingOrder;
        [Min(0f)] [SerializeField] private float respawnDelay = 0.65f;
        [SerializeField] private UnityEvent onCheckpointActivated = new UnityEvent();
        [SerializeField] private UnityEvent onRespawnStarted = new UnityEvent();
        [SerializeField] private UnityEvent onRespawnFinished = new UnityEvent();
        [SerializeField] private UnityEvent onProgressReset = new UnityEvent();

        private CheckpointSnapshot initial;
        private CheckpointSnapshot current;
        private ICheckpointStore store;
        private Coroutine respawnRoutine;
        private bool initialized;
        public event Action<CheckpointSnapshot> CheckpointChanged;
        public event Action<string> StorageMessage;
        public RespawnTarget Player => player;
        public CheckpointSnapshot Current => current;
        public bool HasCheckpoint { get; private set; }
        public bool IsRespawning { get; private set; }
        public int RespawnCount { get; private set; }
        public string LastStorageMessage { get; private set; } = "";
        public string SceneKey => !string.IsNullOrWhiteSpace(sceneKey) ? sceneKey :
            (string.IsNullOrEmpty(gameObject.scene.path) ? gameObject.scene.name : gameObject.scene.path);
        public string SaveSlot { get => saveSlot; set => saveSlot = value ?? "player-01"; }
        public bool PersistToDisk { get => persistToDisk; set => persistToDisk = value; }
        public bool RestoreOnStart { get => restoreOnStart; set => restoreOnStart = value; }
        public bool RequireIncreasingOrder { get => requireIncreasingOrder; set => requireIncreasingOrder = value; }
        public float RespawnDelay { get => respawnDelay; set => respawnDelay = Mathf.Max(0, value); }
        public UnityEvent OnCheckpointActivated => onCheckpointActivated;
        public UnityEvent OnRespawnStarted => onRespawnStarted;
        public UnityEvent OnRespawnFinished => onRespawnFinished;
        public UnityEvent OnProgressReset => onProgressReset;

        private void Awake() { if (player) Initialize(player); }
        private void Start()
        {
            if (!initialized)
            { Debug.LogError("Checkpoint System: assign a Respawn Target to the manager.", this); return; }
            if (persistToDisk && restoreOnStart) LoadSavedCheckpoint(true);
        }

        /// <summary>Bind a single player and capture its initial pose. Call once before using the manager.</summary>
        public bool Initialize(RespawnTarget target)
        {
            if (initialized) return player == target;
            if (!target || (target.Manager && target.Manager != this)) return false;
            player = target;
            target.Manager = this;
            var spawn = initialSpawn ? initialSpawn : target.transform;
            initial = CheckpointSnapshot.Create(SceneKey, "__initial__", "Start platform", -1,
                spawn.position, spawn.rotation);
            current = initial;
            store = store ?? new JsonCheckpointStore();
            initialized = true;
            return true;
        }

        public void SetStore(ICheckpointStore customStore) => store = customStore ?? throw new ArgumentNullException(nameof(customStore));

        /// <summary>Void-returning adapters for Inspector UnityEvent bindings.</summary>
        public void RespawnPlayer() { RequestRespawn(); }
        public void SaveProgress() { SaveCurrent(); }
        public void LoadProgress() { LoadSavedCheckpoint(); }
        public void ClearProgress() { ResetProgress(); }

        public bool TryActivate(Checkpoint checkpoint, RespawnTarget target)
        {
            if (!isActiveAndEnabled || !initialized || IsRespawning || target != player || !checkpoint
                || !checkpoint.isActiveAndEnabled || checkpoint.gameObject.scene != gameObject.scene) return false;
            if (HasCheckpoint && (current.checkpointId == checkpoint.Id ||
                (requireIncreasingOrder && checkpoint.Order <= current.order))) return false;
            CheckpointSnapshot next = checkpoint.CreateSnapshot(SceneKey);
            if (!next.IsValid) return false;
            current = next;
            HasCheckpoint = true;
            if (persistToDisk) SaveCurrent();
            CheckpointChanged?.Invoke(current);
            onCheckpointActivated.Invoke();
            checkpoint.NotifyActivated();
            return true;
        }

        public bool SaveCurrent()
        {
            if (!initialized || !HasCheckpoint) return false;
            bool success = store.TrySave(saveSlot, current, out string error);
            Report(error ?? (success ? "Checkpoint saved to disk." : "Checkpoint save failed."));
            return success;
        }

        public bool LoadSavedCheckpoint(bool teleport = true)
        {
            if (!initialized || IsRespawning) return false;
            bool success = store.TryLoad(saveSlot, SceneKey, out CheckpointSnapshot saved, out string error);
            if (!success || !saved.IsValid || saved.sceneKey != SceneKey)
            { Report(error ?? "No valid checkpoint saved for this scene and slot."); return false; }
            current = saved;
            HasCheckpoint = true;
            if (teleport && player) player.Teleport(current.position, current.rotation);
            CheckpointChanged?.Invoke(current);
            Report(error ?? "Checkpoint restored from disk.");
            return true;
        }

        /// <summary>Requests one respawn. Repeated requests during the delay are ignored.</summary>
        public bool RequestRespawn()
        {
            if (!initialized || !player || !player.isActiveAndEnabled || !isActiveAndEnabled || IsRespawning) return false;
            IsRespawning = true;
            player.Suspend();
            onRespawnStarted.Invoke();
            if (!isActiveAndEnabled || !IsRespawning) return false;
            respawnRoutine = StartCoroutine(Respawn());
            return true;
        }

        private IEnumerator Respawn()
        {
            // Unscaled time allows recovery while a pause menu has set timeScale to zero.
            if (respawnDelay > 0f) yield return new WaitForSecondsRealtime(respawnDelay);
            if (player && player.isActiveAndEnabled)
            {
                player.Teleport(current.position, current.rotation);
                RespawnCount++;
            }
            if (player) player.Resume();
            IsRespawning = false;
            respawnRoutine = null;
            onRespawnFinished.Invoke();
        }

        /// <summary>Deletes this slot/scene only, then restores the initial pose in memory.</summary>
        public bool ResetProgress(bool teleport = true)
        {
            if (!initialized) return false;
            if (!store.TryDelete(saveSlot, SceneKey, out string error)) { Report(error); return false; }
            CancelRespawn();
            HasCheckpoint = false;
            current = initial;
            RespawnCount = 0;
            if (teleport && player) player.Teleport(initial.position, initial.rotation);
            CheckpointChanged?.Invoke(current);
            onProgressReset.Invoke();
            Report("Progress cleared for this scene and slot.");
            return true;
        }

        private void Report(string message) { LastStorageMessage = message ?? ""; StorageMessage?.Invoke(LastStorageMessage); }
        private void CancelRespawn()
        {
            if (respawnRoutine != null) StopCoroutine(respawnRoutine);
            respawnRoutine = null;
            if (player) player.Resume();
            IsRespawning = false;
        }
        private void OnDisable() { CancelRespawn(); }
        private void OnDestroy() { if (player && player.Manager == this) player.Manager = null; }
    }
}
