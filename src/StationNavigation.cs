namespace Baiye.CompactTransit
{
    public static class StationNavigation
    {
        public static bool TryResolve(NavGrid.Transition transition, int cell, NavTable table, out int destination)
        {
            destination = Grid.InvalidCell;
            if (transition.start != NavType.Tube && transition.end != NavType.Tube) return false;
            StationRegistry.TryGet(cell, out var from);
            int target = Grid.OffsetCell(cell, transition.x, transition.y);
            StationRegistry.TryGet(target, out var to);
            if (from == null && to == null) return false;
            if (!Grid.IsCellOffsetValid(cell, transition.x, transition.y) || !table.IsValid(target, transition.end)) return true;

            if (transition.start == NavType.Floor && transition.end == NavType.Tube)
            {
                // The native TubeTraveller still reserves and pays at this floor
                // cell. The new one-cell entry feeds the shared two-cell cabin.
                if (from != null && cell == from.Bottom && transition.x == 0 && transition.y == 1
                    && from.Connected && HeadroomClear(from)) destination = target;
                return true;
            }
            if (transition.start == NavType.Tube && transition.end == NavType.Floor)
            {
                if (from != null && cell == from.Upper && target == from.Bottom && HeadroomClear(from)
                    && HasSideLanding(from, table)) destination = target;
                return true;
            }
            if (transition.start != NavType.Tube || transition.end != NavType.Tube
                || transition.x != 0 || System.Math.Abs(transition.y) != 1) return true;
            if (from != null && to != null)
            {
                if (from.Bottom == to.Bottom) destination = target;
            }
            else
            {
                var station = from ?? to;
                int internalCell = from != null ? cell : target;
                int externalCell = from != null ? target : cell;
                if ((internalCell == station.Upper && externalCell == station.TopPort && station.TopConnected)
                    || (internalCell == station.Bottom && externalCell == station.BottomPort && station.BottomConnected)) destination = target;
            }
            return true;
        }

        private static bool HeadroomClear(StationTopology station)
        {
            var blockers = Grid.BuildFlags.Solid | Grid.BuildFlags.DupeImpassable;
            return (Grid.BuildMasks[station.Bottom] & blockers) == 0 && (Grid.BuildMasks[station.Upper] & blockers) == 0;
        }

        public static bool HasSideLanding(StationTopology station, NavTable table)
        {
            var blockers = Grid.BuildFlags.Solid | Grid.BuildFlags.DupeImpassable;
            for (int x = -1; x <= 1; x += 2)
            {
                if (!Grid.IsCellOffsetValid(station.Bottom, x, 0) || !Grid.IsCellOffsetValid(station.Upper, x, 0)) continue;
                int foot = Grid.OffsetCell(station.Bottom, x, 0), head = Grid.OffsetCell(station.Upper, x, 0);
                if (table.IsValid(foot, NavType.Floor) && (Grid.BuildMasks[foot] & blockers) == 0
                    && (Grid.BuildMasks[head] & blockers) == 0) return true;
            }
            return false;
        }

        // A private array identity marks only the transition we append. Other
        // mods may add their own floor->tube steps with identical geometry.
        internal static readonly NavOffset[] EntryMarker = new NavOffset[0];
        public static bool IsCompactEntry(NavGrid.Transition transition) => transition.start == NavType.Floor
            && transition.end == NavType.Tube && transition.x == 0 && transition.y == 1
            && object.ReferenceEquals(transition.validNavOffsets, EntryMarker);
    }
}
