using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Processes;

[SystemOrder(25)]
public class CombatStateProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            if (actor.Health.IsDead) continue;

            actor.Combat.TickImpulse(deltaTime);
            actor.Combat.TickCombo(deltaTime);
        }
    }
}
