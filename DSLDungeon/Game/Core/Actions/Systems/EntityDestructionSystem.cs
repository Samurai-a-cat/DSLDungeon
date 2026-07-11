using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Core.Actions.Systems;

[SystemOrder(95)]
public class EntityDestructionSystem : IGameSystem
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
