using UnityEngine;

namespace CheckpointSystem
{
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Checkpoint System/Respawn Zone")]
    public sealed class RespawnZone : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayers = ~0;
        private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnTriggerEnter(Collider other)
        {
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0) return;
            var target = other.GetComponentInParent<RespawnTarget>();
            if (target && target.Manager) target.Manager.RequestRespawn();
        }
    }
}
