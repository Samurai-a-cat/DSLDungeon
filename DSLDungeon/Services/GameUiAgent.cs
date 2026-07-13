using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Services;

public class GameUiAgent
{
    // --- ПУБЛИЧНЫЕ БУФЕРЫ ВЫВОДА ---
    public List<EntitySnapshot> Entities { get; } = new();
    public List<TileSnapshot> MapTiles { get; private set; } = new();
    public List<ActorSnapshot> Actors { get; } = new();
    public List<MovementArrowSnapshot> ActiveArrows { get; } = new();
    public List<AttackArrowSnapshot> ActiveAttacks { get; } = new();
    public List<FloatingTextParticle> ActiveParticles { get; } = new();
    public List<string> Logs { get; } = new();

    // --- ПАРАМЕТРЫ ВВОДА ---
    public float PendingSpeed { get; set; } = 1.0f;

    // --- ВНУТРЕННИЙ КЭШ ---
    private readonly Dictionary<EntityId, EntitySnapshot> _persistentInspector = new();
    private readonly Dictionary<EntityId, ActorSnapshot> _persistentActors = new();
    private readonly List<TileSnapshot> _cachedMapTiles = new();
    private readonly List<FloatingTextParticle> _particles = new();

    private readonly HashSet<EntityId> _activeIdsCache = new();
    private readonly List<EntityId> _idsToRemoveCache = new();

    private bool _mapInitialized;

    public event Action? OnRenderTick;
    public event Action? OnLogChanged;
    private int _lastLogHash; 

    public IReadOnlyList<TileSnapshot> AllCachedTiles => _cachedMapTiles;

    public void SyncFromGame(WorldState world, float deltaTime)
    {
        SyncLogs(world);
        SyncEntityInspector(world);
        SyncMapGeometry(world);
        SyncActiveParticles(world, deltaTime);
        SyncActorsAndVectors(world);

        OnRenderTick?.Invoke();
    }

    #region Приватные шаги конвейера рендеринга

    private void SyncLogs(WorldState world)
    {
        // Проверяем, изменились ли логи
        int currentHash = world.LogMessages.Count;
        for (int i = 0; i < world.LogMessages.Count; i++)
            currentHash = HashCode.Combine(currentHash, world.LogMessages[i].GetHashCode());
    
        if (currentHash == _lastLogHash) return;  // ничего не менялось
        _lastLogHash = currentHash;

        Logs.Clear();
        int logCount = world.LogMessages.Count;
        for (int i = 0; i < logCount; i++)
        {
            Logs.Add(world.LogMessages[i]);
        }
    
        OnLogChanged?.Invoke();  // <-- пуш-уведомление в Blazor
    }

    private void SyncEntityInspector(WorldState world)
    {
        _activeIdsCache.Clear();
        
        foreach (var entity in world.GetAllEntities())
        {
            _activeIdsCache.Add(entity.Id);
            
            var health = entity.GetComponent<HealthComponent>();
            
            if (_persistentInspector.TryGetValue(entity.Id, out var snapshot))
            {
                snapshot.Position = entity.Position.ToString();
                snapshot.CurrentHp = health.CurrentHp;
                snapshot.MaxHp = health.MaxHp;
                snapshot.IsDead = health.IsDead;
            }
            else
            {
                _persistentInspector[entity.Id] = new EntitySnapshot
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Position = entity.Position.ToString(),
                    CurrentHp = health.CurrentHp,
                    MaxHp = health.MaxHp,
                    IsDead = health.IsDead
                };
            }
        }

        _idsToRemoveCache.Clear();
        foreach (var key in _persistentInspector.Keys)
        {
            if (!_activeIdsCache.Contains(key)) _idsToRemoveCache.Add(key);
        }
        
        int removeCount = _idsToRemoveCache.Count;
        for (int i = 0; i < removeCount; i++)
        {
            _persistentInspector.Remove(_idsToRemoveCache[i]);
        }

        Entities.Clear();
        foreach (var snap in _persistentInspector.Values)
        {
            Entities.Add(snap);
        }
    }

    private void SyncMapGeometry(WorldState world)
    {
        if (!_mapInitialized)
        {
            InitializeMapGeometry(world);
            _mapInitialized = true;
        }

        MapTiles = _cachedMapTiles; 
    }

    private void SyncActiveParticles(WorldState world, float deltaTime)
    {
        int triggerCount = world.PendingDamageTriggers.Count;
        for (int i = 0; i < triggerCount; i++)
        {
            var trigger = world.PendingDamageTriggers[i];
            var (cx, cy) = GetTilePixelCenter(trigger.Coords);
            
            _particles.Add(new FloatingTextParticle
            {
                X = cx,
                Y = cy - 20f,
                Text = trigger.Text,
                ColorClass = trigger.Type == "Heal" ? "particle-heal" : "particle-dmg"
            });
        }
        world.PendingDamageTriggers.Clear();

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Y += p.VelocityY * deltaTime;
            p.Lifetime -= deltaTime;
            
            if (p.Lifetime <= 0)
            {
                _particles.RemoveAt(i);
            }
        }
        
        ActiveParticles.Clear();
        ActiveParticles.AddRange(_particles);
    }

    private void SyncActorsAndVectors(WorldState world)
    {
        Actors.Clear();
        ActiveArrows.Clear();
        ActiveAttacks.Clear();

        var entities = world.GetAllEntities();
        foreach (var entity in entities)
        {
            if (entity is Actor actor)
            {
                var health = actor.GetComponent<HealthComponent>();
                if (health is { IsDead: true }) continue;

                var (startX, startY) = GetTilePixelCenter(actor.Position);
                float visualX = startX;
                float visualY = startY;

                var activeEvent = actor.Queue.GetActiveEvent();

                if (activeEvent is MoveEvent moveEvent)
                {
                    var (targetX, targetY) = GetTilePixelCenter(moveEvent.TargetCoords);
                    float progress = Math.Clamp(moveEvent.ElapsedTime / moveEvent.Duration, 0f, 1f);

                    visualX = startX + (targetX - startX) * progress;
                    visualY = startY + (targetY - startY) * progress;

                    ActiveArrows.Add(new MovementArrowSnapshot
                    {
                        StartX = startX,
                        StartY = startY,
                        TargetX = targetX,
                        TargetY = targetY,
                        ActorClass = actor.Name.Contains("Рыцарь") ? "hero-actor" : "orc-actor"
                    });
                }
                else if (activeEvent is MeleeAttackEvent attackEvent)
                {
                    if (world.TryGetEntity(attackEvent.TargetId, out var target))
                    {
                        var (targetX, targetY) = GetTilePixelCenter(target.Position);
                        
                        ActiveAttacks.Add(new AttackArrowSnapshot
                        {
                            StartX = startX,
                            StartY = startY,
                            TargetX = targetX,
                            TargetY = targetY
                        });
                    }
                }

                int maxHp = health.MaxHp;
                int currentHp = health.CurrentHp;
                float hpPercent = (float)currentHp / maxHp;

                if (!_persistentActors.TryGetValue(actor.Id, out var actorSnap))
                {
                    actorSnap = new ActorSnapshot { Id = actor.Id };
                    _persistentActors[actor.Id] = actorSnap;
                }

                actorSnap.Name = actor.Name;
                actorSnap.Label = actor.Name.Contains("Рыцарь") ? "♞" : "👹";
                actorSnap.Class = actor.Name.Contains("Рыцарь") ? "hero-actor" : "orc-actor";
                
                actorSnap.PixelX = visualX;
                actorSnap.PixelY = visualY;
                actorSnap.HpPercent = hpPercent;

                Actors.Add(actorSnap);
            }
        }

        if (_persistentActors.Count > Actors.Count * 2) 
        {
            _persistentActors.Clear(); 
        }
    }

    #endregion

    #region Вспомогательная математика гексов

    private (float X, float Y) GetTilePixelCenter(HexCoords coords)
    {
        const float size = 36f; 
        const float offsetX = 600f;
        const float offsetY = 600f; 
        const float sqrt3 = 1.73205f;

        float cx = offsetX + size * (sqrt3 * coords.Q + sqrt3 / 2f * coords.R);
        float cy = offsetY + size * (1.5f * coords.R);
        return (cx, cy);
    }

    private void InitializeMapGeometry(WorldState world)
    {
        _cachedMapTiles.Clear();
        const float size = 36f; 
        const float offsetX = 600f; 
        const float offsetY = 600f; 
        const float sqrt3 = 1.73205f;

        float width = sqrt3 * size;
        float height = 2f * size;

        foreach (var tile in world.Map.GetAllTiles())
        {
            float cx = offsetX + size * (sqrt3 * tile.Coords.Q + sqrt3 / 2f * tile.Coords.R);
            float cy = offsetY + size * (1.5f * tile.Coords.R);

            _cachedMapTiles.Add(new TileSnapshot
            {
                Q = tile.Coords.Q,
                R = tile.Coords.R,
                TerrainClass = tile.Terrain.ToString().ToLower(),
                Left = cx - (width / 2f),
                Top = cy - (height / 2f),
                Width = width,
                Height = height,
                CenterX = cx,
                CenterY = cy
            });
        }
    }

    #endregion
}

#region --- ТАКТИЧЕСКИЕ VIEW-MODELS ДЛЯ UI ---

public class EntitySnapshot
{
    public EntityId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public bool IsDead { get; set; }
}

public class TileSnapshot
{
    public int Q { get; set; }
    public int R { get; set; }
    public string TerrainClass { get; set; } = string.Empty;
    
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public double CenterX { get; set; }
    public double CenterY { get; set; }
}

public class ActorSnapshot
{
    public EntityId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    
    public double PixelX { get; set; }
    public double PixelY { get; set; }
    public float HpPercent { get; set; }
}

public class MovementArrowSnapshot
{
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double TargetX { get; set; }
    public double TargetY { get; set; }
    public string ActorClass { get; set; } = string.Empty;
}

public class AttackArrowSnapshot
{
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double TargetX { get; set; }
    public double TargetY { get; set; }
}

public class FloatingTextParticle
{
    public float X { get; set; }
    public float Y { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ColorClass { get; set; } = string.Empty;
    public float VelocityY { get; set; } = -35f; 
    public float Lifetime { get; set; } = 1.0f;  

    public float Opacity => Math.Clamp(Lifetime / 1.0f, 0f, 1f);
}

#endregion