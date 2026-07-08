using System.Diagnostics.CodeAnalysis;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public class WorldState
{
    public HexMap Map { get; }
    private readonly Dictionary<EntityId, Entity> _entities = new();

    public WorldState(HexMap map)
    {
        Map = map;
    }

    public void SpawnEntity(Entity entity)
    {
        _entities[entity.Id] = entity;
    }

    public void RemoveEntity(EntityId id)
    {
        _entities.Remove(id);
    }

    public bool TryGetEntity(EntityId id, [NotNullWhen(true)] out Entity? entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    public IEnumerable<Entity> GetAllEntities() => _entities.Values;

    // Вспомогательный метод: возвращает только существ (Actor)
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

    /// <summary>
    /// Найти любой объект на конкретной координате (например, чтобы проверить, нет ли там стены, двери или врага)
    /// </summary>
    public Entity? GetEntityAt(HexCoords coords)
    {
        foreach (var entity in _entities.Values)
        {
            if (entity.Position == coords)
            {
                // Если у объекта есть здоровье, проверяем, что он не "мертв" (разрушен)
                if (entity.Health != null && entity.Health.IsDead) continue;
                return entity;
            }
        }
        return null;
    }
}