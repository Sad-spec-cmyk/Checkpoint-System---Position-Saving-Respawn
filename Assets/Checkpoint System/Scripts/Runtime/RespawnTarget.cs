using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CheckpointSystem
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Checkpoint System/Respawn Target")]
    public sealed class RespawnTarget : MonoBehaviour
    {
        [Tooltip("Movement, AI or camera scripts temporarily disabled during a respawn delay. Do not include this component or the manager.")]
        [SerializeField] private Behaviour[] suspendDuringRespawn = new Behaviour[0];
        [SerializeField] private UnityEvent onTeleported = new UnityEvent();
        private readonly Dictionary<Behaviour, bool> previousStates = new Dictionary<Behaviour, bool>();
        private bool suspended;
        public CheckpointManager Manager { get; internal set; }
        public UnityEvent OnTeleported => onTeleported;

        internal void Suspend()
        {
            if (suspended) return;
            suspended = true;
            previousStates.Clear();
            for (int i = 0; i < suspendDuringRespawn.Length; i++)
            {
                Behaviour behaviour = suspendDuringRespawn[i];
                if (!behaviour || behaviour == this || behaviour is CheckpointManager) continue;
                if (previousStates.ContainsKey(behaviour)) continue;
                previousStates.Add(behaviour, behaviour.enabled);
                behaviour.enabled = false;
            }
        }

        internal void Resume()
        {
            if (!suspended) return;
            suspended = false;
            foreach (var entry in previousStates) if (entry.Key) entry.Key.enabled = entry.Value;
            previousStates.Clear();
        }

        /// <summary>Moves the root safely and clears Rigidbody velocity. Custom velocity is reset via OnTeleported.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            var controller = GetComponent<CharacterController>();
            var body = GetComponent<Rigidbody>();
            bool controllerWasEnabled = controller && controller.enabled;
            if (controllerWasEnabled) controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (body)
            {
                body.position = position;
                body.rotation = rotation;
                if (!body.isKinematic)
                {
#if UNITY_6000_0_OR_NEWER
                    body.linearVelocity = Vector3.zero;
#else
                    body.velocity = Vector3.zero;
#endif
                    body.angularVelocity = Vector3.zero;
                }
            }
            Physics.SyncTransforms();
            if (controllerWasEnabled) controller.enabled = true;
            onTeleported.Invoke();
        }

        private void OnDisable() { Resume(); }
    }
}
