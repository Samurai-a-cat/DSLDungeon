using System;

namespace DSLDungeon.Game.Grid;

public readonly record struct HexCoords(int Q, int R)
{
    public int Q { get; } = Q; 
    public int R { get; } = R; 
    public int S => -Q - R; 

    public int DistanceTo(HexCoords other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    }

    public override string ToString() => $"({Q}, {R})";

    public static readonly HexCoords[] Directions = 
    {
        new(1, 0), new(1, -1), new(0, -1), 
        new(-1, 0), new(-1, 1), new(0, 1)
    };

    public HexCoords GetNeighbor(int direction)
    {
        var dir = Directions[direction % 6];
        return new HexCoords(Q + dir.Q, R + dir.R);
    }

    public static HexCoords operator -(HexCoords a, HexCoords b) => 
        new(a.Q - b.Q, a.R - b.R);
}
