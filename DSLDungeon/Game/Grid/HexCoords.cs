using System;

namespace DSLDungeon.Game.Grid;

public readonly record struct HexCoords(int Q, int R)
{
    // +Q направлена вправо-вниз (Юго-Восток)
    public int Q { get; } = Q; 
    
    // +R направлена строго вниз (Юг). Соответственно, -R направлена строго вверх (Север)
    public int R { get; } = R; 
    
    // Третья кубическая координата. Рассчитывается автоматически (Q + R + S = 0).
    // +S направлена влево-вверх (Северо-Запад).
    public int S => -Q - R; 

    /// <summary>
    /// Вычисляет расстояние в шагах сетки между двумя гексами.
    /// </summary>
    public int DistanceTo(HexCoords other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    }

    public override string ToString() => $"({Q}, {R})";


    /// <summary>
    /// Направления смещения для 6 соседних гексов.
    /// </summary>
    public static readonly HexCoords[] Directions = 
    {
        new(1, 0),   // Направление 0
        new(1, -1),  // Направление 1
        new(0, -1),  // Направление 2
        new(-1, 0),  // Направление 3
        new(-1, 1),  // Направление 4
        new(0, 1)    // Направление 5
    };

    /// <summary>
    /// Возвращает координаты соседнего гекса по индексу направления (0-5).
    /// </summary>
    public HexCoords GetNeighbor(int direction)
    {
        var dir = Directions[direction % 6];
        return new HexCoords(Q + dir.Q, R + dir.R);
    }
}