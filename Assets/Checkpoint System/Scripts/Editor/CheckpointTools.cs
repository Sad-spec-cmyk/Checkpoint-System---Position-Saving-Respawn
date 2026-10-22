using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheckpointSystem.Editor
{
    public static class CheckpointTools
    {
        private const string AssetRoot = "Assets/Checkpoint System";
        private const string ScenePath = AssetRoot + "/Scenes/Checkpoint_Showcase.unity";

        [MenuItem("Tools/Checkpoint System/Open Demo Scene")]
        public static void OpenDemo()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/Checkpoint System/Open English Manual")]
        public static void OpenManual() => Application.OpenURL(Path.GetFullPath(AssetRoot + "/Documentation/Checkpoint-System-Manual.pdf"));

        [MenuItem("Tools/Checkpoint System/Validate Open Scenes")]
        public static void Validate()
        {
            int issues = 0;
            var keys = new HashSet<string>();
            foreach (Checkpoint checkpoint in Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string key = checkpoint.gameObject.scene.handle + ":" + checkpoint.Id;
                if (string.IsNullOrWhiteSpace(checkpoint.Id) || !keys.Add(key))
                { Debug.LogError("Checkpoint ID is empty or duplicated. Use Generate New Checkpoint ID in the component context menu.", checkpoint); issues++; }
                var box = checkpoint.GetComponent<BoxCollider>();
                if (!box || !box.isTrigger)
                { Debug.LogError("Checkpoint requires a trigger BoxCollider.", checkpoint); issues++; }
                var body = checkpoint.GetComponentInParent<Rigidbody>();
                if (!body || !body.isKinematic || body.useGravity)
                { Debug.LogWarning("Use a kinematic, gravity-free Rigidbody on this trigger when the player uses a CharacterController.", checkpoint); issues++; }
                if (checkpoint.SpawnPoint == checkpoint.transform)
                { Debug.LogWarning("Assign a separate, safe Spawn Point for the player's root.", checkpoint); issues++; }
            }
            foreach (CheckpointManager manager in Object.FindObjectsByType<CheckpointManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!manager.Player) { Debug.LogError("Assign a Respawn Target to this manager.", manager); issues++; }
            Debug.Log("Checkpoint System validation: " + issues + " issue(s).");
        }

        [MenuItem("GameObject/Checkpoint System/Checkpoint", false, 10)]
        private static void CreateCheckpoint()
        {
            var root = new GameObject("Checkpoint");
            Undo.RegisterCreatedObjectUndo(root, "Create Checkpoint");
            var checkpoint = root.AddComponent<Checkpoint>();
            root.GetComponent<BoxCollider>().size = new Vector3(3, 3, 3);
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var spawn = new GameObject("Spawn Point").transform;
            spawn.SetParent(root.transform); spawn.localPosition = Vector3.up * 1.1f;
            checkpoint.Configure(System.Guid.NewGuid().ToString("N"), "Checkpoint", 1, spawn);
            Selection.activeGameObject = root;
        }
    }
}
