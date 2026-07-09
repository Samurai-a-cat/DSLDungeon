using System.Diagnostics.CodeAnalysis;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities.Particles;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public class WorldState
{
    public HexMap Map { get; }
    
    private readonly Dictionary<EntityId, Entity> _entities = new();
    
    // 1. Создаем кэшированный список для живых акторов
    private readonly List<Actor> _activeActors = new();
    
    public List<VisualDamageTrigger> PendingDamageTriggers { get; } = new();
    
    public SystemsRegistry Systems { get; } = new();
    public EventQueue WorldQueue { get; } = new();
    private readonly HashSet<EntityId> _entitiesToDespawn = new();

    public List<string> LogMessages { get; } = new();

    public WorldState(HexMap map)
    {
        Map = map;
        Systems.Initialize(); 
    }

    public void AddLog(string message)
    {
        string timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogMessages.Add(timestamped);
        if (LogMessages.Count > 40)
        {
            LogMessages.RemoveAt(0);
        }
    }

    public void SpawnEntity(Entity entity)
    {
        _entities[entity.Id] = entity;
        
        // 2. Если спавнится Актор — сразу добавляем его в кэш-список
        if (entity is Actor actor)
        {
            _activeActors.Add(actor);
        }
        
        AddLog($"Сущность '{entity.Name}' успешно призвана в мир.");
    }

    public void Despawn(EntityId id)
    {
        if (id.IsNone) return;
        _entitiesToDespawn.Add(id);
    }

    public void FinalizeDespawns()
    {
        if (_entitiesToDespawn.Count == 0) return;

        foreach (var id in _entitiesToDespawn)
        {
            if (_entities.TryGetValue(id, out var entity))
            {
                if (entity is Actor actor)
                {
                    actor.Queue.Clear();
                    actor.Queue.CleanUp(this); 
                    
                    // 3. Безопасно удаляем из кэша акторов при уничтожении
                    _activeActors.Remove(actor);
                }

                _entities.Remove(id);
                EntityIdGenerator.Release(id);

                AddLog($"Сущность '{entity.Name}' прекратила свое существование и удалена из мира.");
            }
        }

        _entitiesToDespawn.Clear();
    }

    public bool TryGetEntity(EntityId id, [NotNullWhen(true)] out Entity? entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    public IEnumerable<Entity> GetAllEntities() => _entities.Values;

    // 4. Возвращаем напрямую ссылку на список без каких-либо ленивых вычислений и аллокаций
    public IReadOnlyList<Actor> GetAllActors() => _activeActors;

    public Entity? GetEntityAt(HexCoords coords)
    {
        // Оптимизируем поиск физических объектов по кэшу акторов, так как статические объекты пока не блокируют клетки
        int count = _activeActors.Count;
        for (int i = 0; i < count; i++)
        {
            var actor = _activeActors[i];
            if (actor.Position == coords)
            {
                if (actor.Health != null && actor.Health.IsDead) continue;
                return actor;
            }
        }
        return null;
    }
}