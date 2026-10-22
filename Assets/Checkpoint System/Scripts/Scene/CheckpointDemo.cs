using UnityEngine;

namespace CheckpointSystem.Demo
{
    /// <summary>The complete showcase interface. IMGUI keeps the example free of UI and input package dependencies.</summary>
    public sealed class CheckpointDemo : MonoBehaviour
    {
        [SerializeField] private CheckpointManager manager;
        [SerializeField] private DemoMover mover;
        [SerializeField] private Checkpoint[] checkpoints;
        private GUIStyle title, subtitle, label, small, number, button, badge;
        private string toast = "Enter a marked terminal zone to save your position.";
        private bool interfaceHidden;
        private bool closeView;
        private Camera sceneCamera;
        private Vector3 overviewPosition;
        private Quaternion overviewRotation;
        private bool touring;
        private int tourIndex;
        private float toastUntil;
        private static readonly Color Navy = new Color32(237, 237, 230, 255);
        private static readonly Color Blue = new Color32(232, 174, 69, 255);
        private static readonly Color Muted = new Color32(180, 187, 188, 255);
        private static readonly Color Panel = new Color(0.04f, 0.052f, 0.057f, 0.92f);
        private static readonly Color Ink = new Color32(21, 25, 27, 255);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            manager.CheckpointChanged += OnChanged;
            manager.OnRespawnStarted.AddListener(OnRespawnStart);
            manager.OnRespawnFinished.AddListener(OnRespawnEnd);
        }
        private void Start()
        {
            sceneCamera = Camera.main;
            if (sceneCamera) { overviewPosition = sceneCamera.transform.position; overviewRotation = sceneCamera.transform.rotation; }
        }
        private void Overview()
        {
            if (sceneCamera) { sceneCamera.transform.SetPositionAndRotation(overviewPosition, overviewRotation); sceneCamera.fieldOfView = 48; }
            closeView = false;
        }
        private void Detail(Vector3 point)
        {
            if (!sceneCamera) return;
            sceneCamera.transform.position = point + new Vector3(4.7f, 3.3f, -5.2f);
            sceneCamera.transform.LookAt(point + new Vector3(0, 1.1f, 0.5f));
            sceneCamera.transform.Translate(-1.1f, 0, 0, Space.Self);
            sceneCamera.fieldOfView = 53;
            closeView = true;
        }
        private void OnDestroy()
        {
            if (!manager) return;
            manager.CheckpointChanged -= OnChanged;
            manager.OnRespawnStarted.RemoveListener(OnRespawnStart);
            manager.OnRespawnFinished.RemoveListener(OnRespawnEnd);
        }
        private void OnChanged(CheckpointSnapshot snapshot) { Notify(manager.HasCheckpoint ? snapshot.displayName + " / position saved" : "Progress reset / ready to explore"); }
        private void OnRespawnStart() { Notify("Respawning / returning to your saved position"); }
        private void OnRespawnEnd() { Notify("Respawn complete / movement restored"); }
        private void Notify(string message) { toast = message; toastUntil = Time.unscaledTime + 4f; }
        private void Update()
        {
            if (!touring || manager.IsRespawning) return;
            var delta = checkpoints[tourIndex].transform.position - mover.transform.position;
            delta.y = 0;
            if (delta.magnitude < 0.25f)
            {
                tourIndex++;
                if (tourIndex == checkpoints.Length) { touring = false; mover.Guided = false; Notify("Route complete / all three checkpoints visited"); }
                else mover.GuideDestination = checkpoints[tourIndex].transform.position;
            }
        }
        private void Tour()
        {
            manager.ResetProgress();
            touring = true; tourIndex = 0; mover.Guided = true;
            mover.GuideDestination = checkpoints[0].transform.position;
        }
        private void StopTour() { touring = false; mover.Guided = false; }
        private void Hazard()
        {
            StopTour();
            manager.Player.Teleport(new Vector3(0, -3f, 0), Quaternion.identity);
            Notify("Fall detected / respawn zone entered");
        }
        private void InitStyles()
        {
            if (title != null) return;
            title = Style(38, Navy, FontStyle.Bold);
            subtitle = Style(21, Muted);
            label = Style(22, Navy, FontStyle.Bold);
            small = Style(17, Muted);
            number = Style(38, Blue, FontStyle.Bold);
            badge = Style(14, Blue, FontStyle.Bold);
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            button.normal.background = Texture2D.whiteTexture; button.hover.background = Texture2D.whiteTexture;
            button.active.background = Texture2D.whiteTexture;
            button.normal.textColor = Color.white; button.hover.textColor = Color.white; button.active.textColor = Color.white;
            button.border = new RectOffset(0, 0, 0, 0);
        }
        private static GUIStyle Style(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, padding = new RectOffset(0, 0, 0, 0) };
            style.normal.textColor = color;
            return style;
        }
        private static void Rect(float x, float y, float w, float h, Color color)
        {
            Color before = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = before;
        }
        private bool Button(float x, float y, float w, float h, string text, bool secondary = false)
        {
            Color before = GUI.backgroundColor;
            GUI.backgroundColor = secondary ? new Color32(50, 59, 63, 255) : Blue;
            button.normal.textColor = secondary ? Navy : Ink;
            button.hover.textColor = button.normal.textColor; button.active.textColor = button.normal.textColor;
            bool pressed = GUI.Button(new Rect(x, y, w, h), text, button);
            GUI.backgroundColor = before;
            return pressed;
        }
        private void OnGUI()
        {
            if (!manager) return;
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Tab) interfaceHidden = !interfaceHidden;
                if (Event.current.keyCode == KeyCode.C) { if (closeView) Overview(); else Detail(manager.Player.transform.position); }
            }
            if (interfaceHidden) return;
            InitStyles();
            Matrix4x4 prior = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            Rect(40, 40, 530, 145, Panel);
            Rect(64, 60, 40, 4, Blue);
            GUI.Label(new Rect(121, 50, 440, 30), "INDUSTRIAL YARD   /   LIVE DEMO", badge);
            GUI.Label(new Rect(64, 91, 510, 55), "CHECKPOINT SYSTEM", title);
            GUI.Label(new Rect(64, 147, 500, 30), "POSITION SAVING  /  PLAYER RECOVERY", small);
            Rect(1565, 56, 291, 44, Panel);
            GUI.Label(new Rect(1590, 69, 270, 30), "LOCAL SESSION     /     V1.0", badge);
            Rect(64, 274, 360, 543, Panel);
            Rect(64, 274, 5, 543, Blue);
            GUI.Label(new Rect(92, 303, 290, 30), "YOUR CHECKPOINT", badge);
            GUI.Label(new Rect(90, 344, 300, 55), manager.HasCheckpoint ? manager.Current.order.ToString("00") : "--", number);
            GUI.Label(new Rect(92, 401, 300, 36), manager.HasCheckpoint ? manager.Current.displayName : "Start platform", label);
            GUI.Label(new Rect(92, 440, 310, 54), manager.IsRespawning ? "Returning to saved position..." : manager.HasCheckpoint ? "Position + rotation secured" : "No checkpoint activated yet", small);
            Rect(92, 486, 304, 1, new Color32(66, 73, 77, 255));
            for (int i = 0; i < checkpoints.Length; i++)
            {
                bool active = manager.HasCheckpoint && manager.Current.checkpointId == checkpoints[i].Id;
                Rect(92, 517 + i * 55, 28, 28, active ? Blue : new Color32(49, 56, 58, 255));
                GUI.Label(new Rect(135, 520 + i * 55, 250, 30), "0" + (i + 1) + "   " + checkpoints[i].DisplayName, active ? label : small);
            }
            GUI.Label(new Rect(92, 697, 300, 30), "RESPAWNS   " + manager.RespawnCount.ToString("00"), badge);
            GUI.Label(new Rect(92, 738, 300, 45), manager.HasCheckpoint ? "LOCAL SAVE  /  JSON" : "ENTER A TERMINAL ZONE", small);
            GUI.enabled = !manager.IsRespawning;
            if (Button(64, 840, 360, 54, touring ? "Stop guided tour" : "Run guided tour")) { if (touring) StopTour(); else Tour(); }
            GUI.enabled = true;
            if (Time.unscaledTime < toastUntil || manager.IsRespawning)
            {
                Rect(550, 838, 970, 56, Panel);
                Rect(550, 838, 5, 56, Blue);
                GUI.Label(new Rect(574, 856, 930, 32), toast, small);
            }
            Rect(64, 933, 1792, 89, Panel);
            GUI.Label(new Rect(90, 955, 520, 30), "WASD / ARROWS   Move     SPACE   Jump", small);
            GUI.Label(new Rect(90, 985, 520, 25), "C   Camera view     TAB   Hide interface", badge);
            GUI.enabled = !manager.IsRespawning;
            if (Button(641, 953, 240, 48, "Respawn   [R]")) { StopTour(); manager.RequestRespawn(); }
            if (Button(897, 953, 240, 48, "Test fall   [F]", true)) Hazard();
            if (Button(1153, 953, 240, 48, "Load save   [L]", true)) { StopTour(); manager.LoadSavedCheckpoint(); }
            if (Button(1409, 953, 240, 48, "Reset   [X]", true)) { StopTour(); manager.ResetProgress(); }
            GUI.enabled = true;
            GUI.Label(new Rect(1678, 970, 160, 32), "3D  /  LOCAL", badge);
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.R) { StopTour(); manager.RequestRespawn(); }
                if (Event.current.keyCode == KeyCode.F) Hazard();
                if (Event.current.keyCode == KeyCode.L) { StopTour(); manager.LoadSavedCheckpoint(); }
                if (Event.current.keyCode == KeyCode.X) { StopTour(); manager.ResetProgress(); }
            }
            GUI.matrix = prior;
        }
    }
}
