using System;
using System.Collections.Generic;
using System.Globalization;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Services;

public class EntitySnapshot
{
    public EntityId Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public bool IsDead { get; set; }
}

public class TileSnapshot
{
    public int Q { get; init; }
    public int R { get; init; }
    public string TerrainClass { get; set; } = string.Empty;
    public string SvgPoints { get; init; } = string.Empty;
    public float CenterX { get; init; }
    public float CenterY { get; init; }
    
    // --- ПРЕДРАССЧИТАННЫЕ СТРОКИ ДЛЯ SVG (Исключают баги локализации с запятой) ---
    public string SvgCenterX => CenterX.ToString("0.0", CultureInfo.InvariantCulture);
    public string SvgCenterY => CenterY.ToString("0.0", CultureInfo.InvariantCulture);
    public string SvgCoordsY => (CenterY + 13).ToString("0.0", CultureInfo.InvariantCulture);
    public string SvgHaloY => (CenterY - 4).ToString("0.0", CultureInfo.InvariantCulture);
    public string SvgIconY => (CenterY + 2).ToString("0.0", CultureInfo.InvariantCulture);
    public string SvgHpX => (CenterX - 12).ToString("0.0", CultureInfo.InvariantCulture);
    public string SvgHpY => (CenterY - 21).ToString("0.0", CultureInfo.InvariantCulture);
    
    public string ActorLabel { get; set; } = string.Empty;
    public string ActorClass { get; set; } = string.Empty;
    public int ActorHpPercent { get; set; } = 100;

    // Динамический расчет ширины полоски здоровья с точкой
    public string SvgHpWidth => (24f * ActorHpPercent / 100f).ToString("0.0", CultureInfo.InvariantCulture);
}

public class GameUiAgent
{
    public List<EntitySnapshot> Entities { get; private set; } = new();
    public List<TileSnapshot> MapTiles { get; private set; } = new();
    public List<string> Logs { get; private set; } = new();
    
    private readonly Dictionary<EntityId, EntitySnapshot> _persistentInspector = new();
    private readonly List<TileSnapshot> _cachedMapTiles = new();
    private bool _mapInitialized = false;

    public float PendingSpeed { get; set; } = 1.0f;

    public event Action? OnRenderTick;

    public void SyncFromGame(WorldState world)
    {
        // 1. Синхронизация логов
        Logs = new List<string>(world.LogMessages);

        // 2. Синхронизация существ
        var activeIds = new HashSet<EntityId>();
        foreach (var entity in world.GetAllEntities())
        {
            activeIds.Add(entity.Id);
            
            if (_persistentInspector.TryGetValue(entity.Id, out var snapshot))
            {
                snapshot.Position = entity.Position.ToString();
                snapshot.CurrentHp = entity.Health?.CurrentHp ?? 0;
                snapshot.MaxHp = entity.Health?.MaxHp ?? 0;
                snapshot.IsDead = entity.Health?.IsDead ?? false;
            }
            else
            {
                _persistentInspector[entity.Id] = new EntitySnapshot
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Position = entity.Position.ToString(),
                    CurrentHp = entity.Health?.CurrentHp ?? 0,
                    MaxHp = entity.Health?.MaxHp ?? 0,
                    IsDead = entity.Health?.IsDead ?? false
                };
            }
        }

        var idsToRemove = new List<EntityId>();
        foreach (var key in _persistentInspector.Keys)
        {
            if (!activeIds.Contains(key))
            {
                idsToRemove.Add(key);
            }
        }
        foreach (var id in idsToRemove)
        {
            _persistentInspector.Remove(id);
        }

        Entities = new List<EntitySnapshot>(_persistentInspector.Values);

        // 3. Инициализация геометрии
        if (!_mapInitialized)
        {
            InitializeMapGeometry(world);
            _mapInitialized = true;
        }

        // 4. Поиск существ на тайлах
        var entityOnTileMap = new Dictionary<HexCoords, Entity>();
        foreach (var entity in world.GetAllEntities())
        {
            if (entity.Health?.IsDead == true) continue;
            entityOnTileMap[entity.Position] = entity;
        }

        int tileCount = _cachedMapTiles.Count;
        for (int i = 0; i < tileCount; i++)
        {
            var tile = _cachedMapTiles[i];
            var coords = new HexCoords(tile.Q, tile.R);

            if (entityOnTileMap.TryGetValue(coords, out var entity))
            {
                tile.ActorLabel = entity.Name.Contains("Рыцарь") ? "♞" : "👹";
                tile.ActorClass = entity.Name.Contains("Рыцарь") ? "hero-actor" : "orc-actor";
                
                int maxHp = entity.Health?.MaxHp ?? 100;
                int currentHp = entity.Health?.CurrentHp ?? 100;
                tile.ActorHpPercent = (int)((float)currentHp / maxHp * 100);
            }
            else
            {
                tile.ActorLabel = string.Empty;
                tile.ActorClass = string.Empty;
                tile.ActorHpPercent = 0;
            }
        }

        MapTiles = _cachedMapTiles; 
        OnRenderTick?.Invoke();
    }

    private void InitializeMapGeometry(WorldState world)
    {
        _cachedMapTiles.Clear();
        const float size = 28f; 
        const float offsetX = 200f; 
        const float offsetY = 180f; 
        const float sqrt3 = 1.73205f;

        foreach (var tile in world.Map.GetAllTiles())
        {
            float cx = offsetX + size * (sqrt3 * tile.Coords.Q + sqrt3 / 2f * tile.Coords.R);
            float cy = offsetY + size * (1.5f * tile.Coords.R);

            _cachedMapTiles.Add(new TileSnapshot
            {
                Q = tile.Coords.Q,
                R = tile.Coords.R,
                TerrainClass = tile.Terrain.ToString().ToLower(),
                SvgPoints = CalculateHexPoints(cx, cy, size),
                CenterX = cx,
                CenterY = cy,
                ActorLabel = string.Empty,
                ActorClass = string.Empty
            });
        }
    }

    private string CalculateHexPoints(float cx, float cy, float r)
    {
        var points = new List<string>(6);
        for (int i = 0; i < 6; i++)
        {
            double angleRad = Math.PI / 180 * (60 * i - 30);
            double px = cx + r * Math.Cos(angleRad);
            double py = cy + r * Math.Sin(angleRad);
            points.Add($"{px.ToString("0.0", CultureInfo.InvariantCulture)},{py.ToString("0.0", CultureInfo.InvariantCulture)}");
        }
        return string.Join(" ", points);
    }
}