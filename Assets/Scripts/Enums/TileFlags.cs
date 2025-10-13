[System.Flags]
public enum TileFlags : ushort
{
    None = 0,
    Wall = 1 << 0,
    WalkPlayer = 1 << 1,
    WalkGhost = 1 << 2,
    Pellet = 1 << 3,
    PowerPellet = 1 << 4,
    GhostHouse = 1 << 5,
    GhostGate = 1 << 6,
    Teleporter = 1 << 7
}