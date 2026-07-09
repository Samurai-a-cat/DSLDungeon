using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public class WorldState
{
    public HexMap Map { get; }
    private readonly Dictionary<EntityId, Entity> _entities = new();
    
    public SystemsRegistry Systems { get; } = new();
    public EventQueue WorldQueue { get; } = new();
    private readonly HashSet<EntityId> _entitiesToDespawn = new();

    // Буфер внутриигровых логов
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
        if (LogMessages.Count > 40) // Ограничиваем историю
        {
            LogMessages.RemoveAt(0);
        }
    }

    public void SpawnEntity(Entity entity)
    {
        _entities[entity.Id] = entity;
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

    public IEnumerable<Actor> GetAllActors()
    {
        foreach (var entity in _entities.Values)
        {
            if (entity is Actor actor)
            {
                yield return actor;
            }
        }
    }

    public Entity? GetEntityAt(HexCoords coords)
    {
        foreach (var entity in _entities.Values)
        {
            if (entity.Position == coords)
            {
                if (entity.Health != null && entity.Health.IsDead) continue;
                return entity;
            }
        }
        return null;
    }
}