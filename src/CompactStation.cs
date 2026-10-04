using HarmonyLib;
using UnityEngine;

namespace Baiye.CompactTransit
{
    // The station owns two vertical native tube sockets and one thin-floor
    // reference. External pipes and crossing links retain native ownership.
    public sealed class CompactStation : KMonoBehaviour, ITravelTubePiece, ISim200ms
    {
        private static readonly Operational.Flag TubeFlag = (Operational.Flag)AccessTools.Field(typeof(TravelTubeEntrance), "tubeConnected").GetValue(null);
        private static readonly System.Reflection.FieldInfo LinkConnected = AccessTools.Field(typeof(UtilityNetworkLink), "connected");
        private int bottom = Grid.InvalidCell;
        private StationTopology topology;
        private Operational operational;
        private readonly PlatformSupport platform = new PlatformSupport();
        private bool networkRegistered;
        private bool cleaning;
        private bool initialVisualRefresh = true;
        public Vector3 Position => transform.GetPosition();
        public bool TopConnected => topology != null && topology.TopConnected;
        public bool BottomConnected => topology != null && topology.BottomConnected;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            bottom = Grid.PosToCell(this);
            if (!Grid.IsCellOffsetValid(bottom, 0, 1)) return;
            Grid.HasTube[bottom] = true; Grid.HasTube[Grid.CellAbove(bottom)] = true;
            Components.ITravelTubePieces.Add(this);
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            operational = GetComponent<Operational>();
            platform.Acquire(bottom);
            Grid.Objects[bottom, (int)ObjectLayer.TravelTubeConnection] = gameObject;
            Grid.Objects[Grid.CellAbove(bottom), (int)ObjectLayer.TravelTubeConnection] = gameObject;
            Grid.Objects[bottom, (int)ObjectLayer.TravelTubeTile] = gameObject;
            Grid.Objects[Grid.CellAbove(bottom), (int)ObjectLayer.TravelTubeTile] = gameObject;
            if (topology == null)
            {
                topology = new StationTopology(bottom, Grid.WidthInCells, false, false);
                StationRegistry.Publish(topology);
            }
            StationTubeConnections.Connect(Game.Instance.travelTubeSystem, bottom);
            networkRegistered = true;
            RefreshPipeVisuals();
            RefreshPorts();
            MarkDirty();
        }
        public void Sim200ms(float dt)
        {
            // Refresh once after prefab spawning finishes, when adjacent pipe
            // visualizers have their native connection manager available.
            if (initialVisualRefresh) { initialVisualRefresh = false; RefreshPipeVisuals(); }
            if (platform.Anchor == Grid.InvalidCell)
            {
                platform.Acquire(bottom);
                if (platform.Anchor != Grid.InvalidCell) MarkDirty();
            }
            RefreshPorts();
        }

        public bool ReadConnections()
        {
            if (cleaning) return false;
            if (bottom == Grid.InvalidCell) bottom = Grid.PosToCell(this);
            bool up = PortConnected(2, 1, UtilityConnections.Down);
            bool down = PortConnected(-1, 0, UtilityConnections.Up);
            if (topology == null || topology.TopConnected != up || topology.BottomConnected != down)
            {
                topology = new StationTopology(bottom, Grid.WidthInCells, up, down);
                StationRegistry.Publish(topology);
                MarkDirty();
                RefreshPipeVisuals();
            }
            return topology.Connected;
        }
        private void RefreshPipeVisuals()
        {
            foreach (int y in new[] { -1, 2 })
            {
                if (!Grid.IsCellOffsetValid(bottom, 0, y)) continue;
                int cell = Grid.OffsetCell(bottom, 0, y);
                var pipe = Grid.Objects[cell, (int)ObjectLayer.TravelTubeTile];
                if (pipe != null) pipe.GetComponent<KAnimGraphTileVisualizer>()?.Refresh();
            }
        }
        private void RefreshPorts()
        {
            bool connected = ReadConnections();
            if (operational != null) operational.SetFlag(TubeFlag, connected);
        }
        private bool PortConnected(int y, int internalY, UtilityConnections towardCabin)
        {
            if (Game.Instance == null || !Grid.IsCellOffsetValid(bottom, 0, y)) return false;
            int port = Grid.OffsetCell(bottom, 0, y);
            if (!Grid.HasTube[port]) return false;
            var go = Grid.Objects[port, (int)ObjectLayer.Building];
            var linkGo = Grid.Objects[port, (int)ObjectLayer.FoundationTile];
            var link = linkGo != null ? linkGo.GetComponent<TravelTubeUtilityNetworkLink>() : null;
            if (link == null && go != null) link = go.GetComponent<TravelTubeUtilityNetworkLink>();
            if (link != null)
            {
                if (!link.isSpawned || link.visualizeOnly || !(bool)LinkConnected.GetValue(link)) return false;
                link.GetCells(out int a, out int b);
                int inside = Grid.OffsetCell(bottom, 0, internalY);
                int outside = Grid.OffsetCell(bottom, 0, y + (y > 0 ? 1 : -1));
                return (a == inside && b == outside) || (b == inside && a == outside);
            }
            var pipe = go != null ? go.GetComponent<TravelTube>() : null;
            if (pipe == null || !pipe.isSpawned) return false;
            // Internal cabin cells are virtual: there must be a real outward
            // connection, not merely an isolated decorative tube segment.
            var connections = Game.Instance.travelTubeSystem.GetConnections(port, true);
            return (connections & ~towardCabin) != 0;
        }
        private void MarkDirty()
        {
            if (Pathfinding.Instance == null || bottom == Grid.InvalidCell) return;
            for (int y = -2; y <= 3; y++)
                for (int x = -1; x <= 1; x++)
                    if (Grid.IsCellOffsetValid(bottom, x, y)) Pathfinding.Instance.AddDirtyNavGridCell(Grid.OffsetCell(bottom, x, y));
        }
        protected override void OnCleanUp()
        {
            cleaning = true;
            StationRegistry.Remove(bottom, Grid.WidthInCells);
            platform.Release();
            if (networkRegistered && Game.Instance != null)
            {
                StationTubeConnections.Disconnect(Game.Instance.travelTubeSystem, bottom);
                networkRegistered = false;
                RefreshPipeVisuals();
            }
            for (int y = 0; y <= 1; y++)
            {
                if (!Grid.IsCellOffsetValid(bottom, 0, y)) continue;
                int cell = Grid.OffsetCell(bottom, 0, y);
                if (Grid.Objects[cell, (int)ObjectLayer.TravelTubeConnection] == gameObject)
                    Grid.Objects[cell, (int)ObjectLayer.TravelTubeConnection] = null;
                if (Grid.Objects[cell, (int)ObjectLayer.TravelTubeTile] == gameObject)
                    Grid.Objects[cell, (int)ObjectLayer.TravelTubeTile] = null;
                Grid.HasTube[cell] = false;
            }
            Components.ITravelTubePieces.Remove(this);
            MarkDirty();
            base.OnCleanUp();
        }
    }
}
