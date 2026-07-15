using System.Diagnostics.CodeAnalysis;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Particles;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public class WorldState
{
    public HexMap Map { get; }

    private readonly Dictionary<EntityId, Entity> _entities = new();
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
        entity.World = this;
        _entities[entity.Id] = entity;

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

    public IReadOnlyList<Actor> GetAllActors() => _activeActors;

    public Entity? GetEntityAt(HexCoords coords)
    {
        int count = _activeActors.Count;
        for (int i = 0; i < count; i++)
        {
            var actor = _activeActors[i];
            if (actor.Position == coords)
            {
                if (actor.GetComponent<HealthComponent>() is { IsDead: true }) continue;
                return actor;
            }
        }
        return null;
    }
}
