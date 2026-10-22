namespace CheckpointSystem
{
    /// <summary>Implement this interface to use a custom save backend. Calls run on the main thread.</summary>
    public interface ICheckpointStore
    {
        bool TrySave(string slot, CheckpointSnapshot snapshot, out string error);
        bool TryLoad(string slot, string sceneKey, out CheckpointSnapshot snapshot, out string error);
        bool TryDelete(string slot, string sceneKey, out string error);
    }
}
