using System.Collections.Concurrent;

namespace Baiye.CompactTransit
{
    // Immutable snapshots: NavGrid.IsValid runs on worker threads. Never access
    // GameObjects, components or a live utility manager from navigation patches.
    public sealed class StationTopology
    {
        public readonly int Bottom, Upper, TopPort, BottomPort;
        public readonly bool TopConnected, BottomConnected;
        public StationTopology(int bottom, int width, bool topConnected, bool bottomConnected)
        {
            Bottom = bottom; Upper = bottom + width;
            TopPort = bottom + 2 * width; BottomPort = bottom - width;
            TopConnected = topConnected; BottomConnected = bottomConnected;
        }
        public bool Connected => TopConnected || BottomConnected;
        public bool Contains(int cell) => cell == Bottom || cell == Upper;
    }

    public static class StationRegistry
    {
        private static readonly ConcurrentDictionary<int, StationTopology> Cells = new ConcurrentDictionary<int, StationTopology>();
        public static void Publish(StationTopology station)
        {
            Cells[station.Bottom] = station; Cells[station.Upper] = station;
        }
        public static bool TryGet(int cell, out StationTopology station) => Cells.TryGetValue(cell, out station);
        public static void Remove(int bottom, int width)
        {
            Cells.TryRemove(bottom, out _); Cells.TryRemove(bottom + width, out _);
        }
        public static void Clear() => Cells.Clear();
    }
}
