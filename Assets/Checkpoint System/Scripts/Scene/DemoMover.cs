using System.Collections.Generic;
using UnityEngine;

namespace CheckpointSystem.Demo
{
    [RequireComponent(typeof(CharacterController), typeof(RespawnTarget))]
    public sealed class DemoMover : MonoBehaviour
    {
        [SerializeField] private float speed = 5.5f;
        [SerializeField] private float jumpHeight = 1.5f;
        private CharacterController controller;
        private RespawnTarget target;
        private readonly HashSet<KeyCode> keys = new HashSet<KeyCode>();
        private float vertical;
        private bool jump;
        public bool Guided { get; set; }
        public Vector3 GuideDestination { get; set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            target = GetComponent<RespawnTarget>();
            target.OnTeleported.AddListener(ResetMotion);
        }
        private void OnDestroy() { if (target) target.OnTeleported.RemoveListener(ResetMotion); }
        private void OnDisable() { keys.Clear(); vertical = 0; jump = false; }
        private void OnApplicationFocus(bool focus) { if (!focus) keys.Clear(); }
        private void ResetMotion() { vertical = 0; jump = false; }
        private void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown) { keys.Add(e.keyCode); if (e.keyCode == KeyCode.Space) jump = true; }
            if (e.type == EventType.KeyUp) keys.Remove(e.keyCode);
        }
        private bool Down(KeyCode key) => keys.Contains(key);
        private void Update()
        {
            if (!controller.enabled || (target.Manager && target.Manager.IsRespawning)) return;
            float x = (Down(KeyCode.D) || Down(KeyCode.RightArrow) ? 1 : 0) - (Down(KeyCode.A) || Down(KeyCode.LeftArrow) ? 1 : 0);
            float z = (Down(KeyCode.W) || Down(KeyCode.UpArrow) ? 1 : 0) - (Down(KeyCode.S) || Down(KeyCode.DownArrow) ? 1 : 0);
#if ENABLE_LEGACY_INPUT_MANAGER
            x = Input.GetAxisRaw("Horizontal"); z = Input.GetAxisRaw("Vertical");
            jump |= Input.GetKeyDown(KeyCode.Space);
#endif
            Vector3 direction = new Vector3(x, 0, z);
            if (Guided)
            {
                direction = GuideDestination - transform.position;
                direction.y = 0;
                direction = direction.magnitude < 0.15f ? Vector3.zero : direction.normalized;
            }
            else direction = Vector3.ClampMagnitude(direction, 1f);
            if (controller.isGrounded && vertical < 0) vertical = -2f;
            if (jump && controller.isGrounded) vertical = Mathf.Sqrt(jumpHeight * 40f);
            jump = false;
            vertical = Mathf.Max(vertical - 20f * Time.deltaTime, -40f);
            controller.Move((direction * speed + Vector3.up * vertical) * Time.deltaTime);
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);
        }
    }
}
