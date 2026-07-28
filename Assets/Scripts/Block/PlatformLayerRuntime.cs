using System.Collections.Generic;

namespace Valley.Level.Generation
{
    [System.Serializable]
    public struct PlatformRecord
    {
        public PlatformBlock prefab;
        public float leftEdgeX;
        public float leftEdgeY;
        public float rotationZ;

        public float rightEdgeX;
        public float rightEdgeY;
    }

    public class PlatformLayerRuntime
    {
        readonly List<PlatformRecord> history = new List<PlatformRecord>();
        public readonly List<PlatformBlock> liveInstances = new List<PlatformBlock>();
        public int liveStartIndex;
        public int historyBaseIndex;

        public int consecutiveSticks;
        public int consecutiveHardGaps;
        public int spawnsSinceSafety;

        public int RecordCount => history.Count;
        public int LastGlobalIndex => historyBaseIndex + history.Count - 1;

        public PlatformRecord GetRecord(int globalIndex) => history[globalIndex - historyBaseIndex];
        public void SetRecord(int globalIndex, PlatformRecord record) => history[globalIndex - historyBaseIndex] = record;
        public void AddRecord(PlatformRecord record) => history.Add(record);

        public void TrimFront(int count)
        {
            if (count <= 0) return;
            history.RemoveRange(0, count);
            historyBaseIndex += count;
        }
    }
}