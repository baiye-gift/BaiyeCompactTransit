namespace Baiye.CompactTransit
{
    // Own exactly one native thin-floor reference. No solid tile or tube
    // occupation is created in the pipe cell below the station.
    public sealed class PlatformSupport
    {
        public int Anchor { get; private set; } = Grid.InvalidCell;
        public void Acquire(int bottom)
        {
            if (!Grid.IsCellOffsetValid(bottom, 0, -1)) return;
            int anchor = Grid.CellBelow(bottom);
            if (Anchor == anchor) return;
            Release();
            // The game's field is a two-bit reference count. Do not take a
            // reference if it is already saturated, then release somebody else's.
            if ((Grid.BuildMasks[anchor] & Grid.BuildFlags.FakeFloor) == Grid.BuildFlags.FakeFloor) return;
            Grid.FakeFloor.Add(anchor);
            Anchor = anchor;
        }
        public void Release()
        {
            if (Grid.IsValidCell(Anchor)) Grid.FakeFloor.Remove(Anchor);
            Anchor = Grid.InvalidCell;
        }
    }
}
