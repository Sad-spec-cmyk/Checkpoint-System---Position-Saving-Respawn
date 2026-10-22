using UnityEngine;

namespace CheckpointSystem
{
    [RequireComponent(typeof(RespawnTarget))]
    [AddComponentMenu("Checkpoint System/Fall Respawn")]
    public sealed class FallRespawn : MonoBehaviour
    {
        [SerializeField] private float minimumWorldY = -8f;
        private RespawnTarget target;
        private void Awake() { target = GetComponent<RespawnTarget>(); }
        private void Update()
        {
            if (transform.position.y < minimumWorldY && target.Manager) target.Manager.RequestRespawn();
        }
    }
}
