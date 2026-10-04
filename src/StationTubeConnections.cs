namespace Baiye.CompactTransit
{
    public static class StationTubeConnections
    {
        // BuildPath requires this concrete native component on every completed
        // object in TravelTubeTile. Keep its connection interface, but let the
        // station remain the sole owner of its animation and two-cell cleanup.
        public static void ConfigureVisualizer(KAnimGraphTileVisualizer visualizer)
        {
            visualizer.connectionSource = KAnimGraphTileVisualizer.ConnectionSource.Tube;
            visualizer.isPhysicalBuilding = true;
            visualizer.skipRefresh = true;
            visualizer.skipCleanup = true;
            visualizer.Connections = UtilityConnections.Up | UtilityConnections.Down;
        }
        public static UtilityConnections Filter(int cell, UtilityConnections connections)
        {
            if (StationRegistry.TryGet(cell, out _)) return connections & (UtilityConnections.Up | UtilityConnections.Down);
            if (Grid.IsCellOffsetValid(cell, -1, 0) && StationRegistry.TryGet(Grid.CellLeft(cell), out _)) connections &= ~UtilityConnections.Left;
            if (Grid.IsCellOffsetValid(cell, 1, 0) && StationRegistry.TryGet(Grid.CellRight(cell), out _)) connections &= ~UtilityConnections.Right;
            return connections;
        }
        // Native pipe prefabs can restore their old one-ended value after the
        // station registers. Include the socket beside a spawned station so
        // both the physical connection and the selected tube visual stay joined.
        public static UtilityConnections IncludeStationSocket(int cell, UtilityConnections connections)
        {
            if (StationRegistry.TryGet(cell, out _)) return connections;
            if (Grid.IsCellOffsetValid(cell, 0, -1) && StationRegistry.TryGet(Grid.CellBelow(cell), out var below)
                && Grid.CellBelow(cell) == below.Upper) connections |= UtilityConnections.Down;
            if (Grid.IsCellOffsetValid(cell, 0, 1) && StationRegistry.TryGet(Grid.CellAbove(cell), out var above)
                && Grid.CellAbove(cell) == above.Bottom) connections |= UtilityConnections.Up;
            return connections;
        }
        public static void Connect(UtilityNetworkManager<TravelTubeNetwork, TravelTube> manager, int bottom)
        {
            var vertical = UtilityConnections.Up | UtilityConnections.Down;
            manager.SetConnections(vertical, bottom, true);
            manager.SetConnections(vertical, Grid.CellAbove(bottom), true);
        }
        public static void Disconnect(UtilityNetworkManager<TravelTubeNetwork, TravelTube> manager, int bottom)
        {
            manager.ClearCell(bottom, true);
            manager.ClearCell(Grid.CellAbove(bottom), true);
        }
    }
}
