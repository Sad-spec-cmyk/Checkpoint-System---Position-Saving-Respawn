using UnityEngine;

namespace CheckpointSystem.Demo
{
    public sealed class CheckpointBeacon : MonoBehaviour
    {
        [SerializeField] private Checkpoint checkpoint;
        [SerializeField] private CheckpointManager manager;
        [SerializeField] private Renderer[] lights = new Renderer[0];
        private MaterialPropertyBlock properties;
        private void Awake() { properties = new MaterialPropertyBlock(); }
        private void Update()
        {
            if (!checkpoint || !manager) return;
            bool active = manager.HasCheckpoint && manager.Current.checkpointId == checkpoint.Id;
            Color tint = active ? new Color(0.12f, 0.8f, 0.35f) : new Color(0.65f, 0.32f, 0.06f);
            properties.SetColor("_Color", tint);
            properties.SetColor("_EmissionColor", tint * (active ? 1.2f : 0.25f));
            foreach (Renderer lightRenderer in lights) if (lightRenderer) lightRenderer.SetPropertyBlock(properties);
        }
    }
}
