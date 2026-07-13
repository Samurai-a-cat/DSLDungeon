namespace DSLDungeon.Game.Grid;

public readonly record struct HexCoords(int Q, int R)
{
    public int S => -Q - R; 

    public int DistanceTo(HexCoords other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    }

    public HexCoords GetNeighbor(int direction)
    {
        return direction switch
        {
            0 => new HexCoords(Q + 1, R),
            1 => new HexCoords(Q + 1, R - 1),
            2 => new HexCoords(Q, R - 1),
            3 => new HexCoords(Q - 1, R),
            4 => new HexCoords(Q - 1, R + 1),
            5 => new HexCoords(Q, R + 1),
            _ => this
        };
    }

    public override string ToString() => $"({Q}, {R})";
}
