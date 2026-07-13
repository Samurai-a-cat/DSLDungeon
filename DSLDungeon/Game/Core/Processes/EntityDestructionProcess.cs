using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Processes;

[SystemOrder(95)]
public class EntityDestructionProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var entity in world.GetAllEntities())
        {
            if (entity is Actor actor && actor.Health.IsDead)
            {
                world.Despawn(entity.Id);
            }
        }
    }
}
