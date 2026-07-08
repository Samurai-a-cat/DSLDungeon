namespace DSLDungeon.Game.Grid;

public readonly struct Tile(HexCoords coords, int elevation, TerrainType terrain)
{
    public HexCoords Coords { get; } = coords;
    public int Elevation { get; } = elevation;
    public TerrainType Terrain { get; } = terrain;

    /// <summary>
    /// Базовое свойство для быстрой проверки проходимости.
    /// </summary>
    public bool IsPassable => Terrain switch
    {
        TerrainType.Ground => true,
        TerrainType.Water => true,
        _ => false
    };

    /// <summary>
    /// Проверяет, блокирует ли тайл видимость.
    /// </summary>
    public bool BlocksLight => Terrain == TerrainType.Wall;
}