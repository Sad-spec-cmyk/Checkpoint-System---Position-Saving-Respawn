using System;
using UnityEngine;
using UnityEngine.Events;

namespace CheckpointSystem
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Checkpoint System/Checkpoint")]
    public sealed class Checkpoint : MonoBehaviour
    {
        [Tooltip("Unique in this scene. Regenerate after duplicating a checkpoint.")]
        [SerializeField] private string checkpointId = "";
        [SerializeField] private string displayName = "Checkpoint";
        [SerializeField] private int order;
        [Tooltip("Player root pose at respawn. Place above solid ground and outside hazards.")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private LayerMask playerLayers = ~0;
        [SerializeField] private UnityEvent onActivated = new UnityEvent();
        public string Id => checkpointId;
        public string DisplayName => displayName;
        public int Order => order;
        public Transform SpawnPoint => spawnPoint ? spawnPoint : transform;
        public UnityEvent OnActivated => onActivated;

        public void Configure(string id, string label, int sequence, Transform spawn)
        { checkpointId = id; displayName = label; order = sequence; spawnPoint = spawn; }
        public bool Activate(RespawnTarget target) => target && target.Manager && target.Manager.TryActivate(this, target);
        public CheckpointSnapshot CreateSnapshot(string sceneKey) => CheckpointSnapshot.Create(sceneKey,
            checkpointId, displayName, order, SpawnPoint.position, SpawnPoint.rotation);
        internal void NotifyActivated() { onActivated.Invoke(); }

        private void OnTriggerEnter(Collider other)
        {
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0) return;
            Activate(other.GetComponentInParent<RespawnTarget>());
        }
        private void Reset()
        {
            checkpointId = Guid.NewGuid().ToString("N");
            GetComponent<BoxCollider>().isTrigger = true;
        }
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(checkpointId)) checkpointId = Guid.NewGuid().ToString("N");
        }
        [ContextMenu("Generate New Checkpoint ID")]
        private void RegenerateId() { checkpointId = Guid.NewGuid().ToString("N"); }
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.7f);
            Gizmos.DrawWireSphere(SpawnPoint.position, 0.3f);
            Gizmos.DrawRay(SpawnPoint.position, SpawnPoint.forward * 1.2f);
        }
    }
}
