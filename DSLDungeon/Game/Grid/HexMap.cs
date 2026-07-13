namespace DSLDungeon.Game.Grid;

public class HexMap
{
    private readonly Dictionary<HexCoords, Tile> _tiles = new();

    public HexMap(int internalRadius)
    {
        if (internalRadius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(internalRadius), "Радиус не может быть отрицательным.");
        }

        GenerateHexagonalPlayableArea(internalRadius);
        BuildBoundaryWalls(TerrainType.UnbreakableWall, 1);
    }

    public HexMap(int internalWidth, int internalHeight)
    {
        if (internalWidth < 1 || internalHeight < 1)
        {
            throw new ArgumentException("Размеры игрового поля должны быть не менее 1x1.");
        }

        GenerateRectangularPlayableArea(internalWidth, internalHeight);
        BuildBoundaryWalls(TerrainType.UnbreakableWall, 1);
    }

    private void GenerateHexagonalPlayableArea(int radius)
    {
        _tiles.Clear();
        for (int q = -radius; q <= radius; q++)
        {
            for (int r = -radius; r <= radius; r++)
            {
                int s = -q - r;
                if (Math.Abs(s) > radius) continue;

                var coords = new HexCoords(q, r);
                _tiles[coords] = new Tile(coords, 0, TerrainType.Ground);
            }
        }
    }

    private void GenerateRectangularPlayableArea(int width, int height)
    {
        _tiles.Clear();
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int q = col - (row + (row & 1)) / 2;
                int r = row;

                var coords = new HexCoords(q, r);
                _tiles[coords] = new Tile(coords, 0, TerrainType.Ground);
            }
        }
    }

    private void BuildBoundaryWalls(TerrainType wallType, int elevation)
    {
        var borderCoords = new HashSet<HexCoords>();

        foreach (var tile in _tiles.Values)
        {
            for (int i = 0; i < 6; i++)
            {
                HexCoords neighbor = tile.Coords.GetNeighbor(i);
                if (!_tiles.ContainsKey(neighbor))
                {
                    borderCoords.Add(neighbor);
                }
            }
        }

        foreach (var coords in borderCoords)
        {
            _tiles[coords] = new Tile(coords, elevation, wallType);
        }
    }

    public void SurroundWith(IEnumerable<HexCoords> targetArea, TerrainType terrainType, int elevation = 0)
    {
        var targetsHash = new HashSet<HexCoords>(targetArea);
        var coordsToModify = new HashSet<HexCoords>();

        foreach (var coords in targetsHash)
        {
            for (int i = 0; i < 6; i++)
            {
                HexCoords neighbor = coords.GetNeighbor(i);
                if (!targetsHash.Contains(neighbor))
                {
                    coordsToModify.Add(neighbor);
                }
            }
        }

        ReplaceTile(terrainType, elevation, coordsToModify);
    }

    private void ReplaceTile(TerrainType terrainType, int elevation, IEnumerable<HexCoords> coordsToModify)
    {
        foreach (var coords in coordsToModify)
        {
            if (_tiles.TryGetValue(coords, out var existingTile))
            {
                if (existingTile.Terrain == TerrainType.UnbreakableWall)
                {
                    continue;
                }

                _tiles[coords] = new Tile(coords, elevation, terrainType);
            }
        }
    }

    public bool TryGetTile(HexCoords coords, out Tile tile)
    {
        return _tiles.TryGetValue(coords, out tile);
    }

    public bool IsInBounds(HexCoords coords)
    {
        return _tiles.ContainsKey(coords);
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        return _tiles.Values;
    }
}
