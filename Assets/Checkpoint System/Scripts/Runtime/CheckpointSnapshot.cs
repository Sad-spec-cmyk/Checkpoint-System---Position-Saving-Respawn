using System;
using UnityEngine;

namespace CheckpointSystem
{
    /// <summary>A versioned, immutable-by-convention copy of a saved spawn pose.</summary>
    [Serializable]
    public struct CheckpointSnapshot
    {
        public int version;
        public string sceneKey;
        public string checkpointId;
        public string displayName;
        public int order;
        public Vector3 position;
        public Quaternion rotation;

        public bool IsValid => version == 1 && !string.IsNullOrWhiteSpace(sceneKey)
            && !string.IsNullOrWhiteSpace(checkpointId) && Finite(position.x)
            && Finite(position.y) && Finite(position.z) && Finite(rotation.x)
            && Finite(rotation.y) && Finite(rotation.z) && Finite(rotation.w)
            && Mathf.Abs(Quaternion.Dot(rotation, rotation) - 1f) < 0.01f;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static CheckpointSnapshot Create(string scene, string id, string label,
            int sequence, Vector3 point, Quaternion facing)
        {
            return new CheckpointSnapshot { version = 1, sceneKey = scene, checkpointId = id,
                displayName = label, order = sequence, position = point, rotation = facing };
        }
    }
}
