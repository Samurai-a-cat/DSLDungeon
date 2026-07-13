using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Core.Processes;

/// <summary>
/// Фоновый процесс: удаляет мёртвые сущности из мира.
/// </summary>
[SystemOrder(95)]
public class EntityDestructionProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var entity in world.GetAllEntities())
        {
            if (entity.GetComponent<HealthComponent>() is { IsDead: true })
            {
                world.Despawn(entity.Id);
            }
        }
    }
}
