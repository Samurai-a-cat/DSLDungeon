using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Core.Processes;

[SystemOrder(15)]
public class BackgroundThreadProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            if (!actor.TryGetComponent<BackgroundThreadData>(out var thread)) continue;
            if (actor.Health.IsDead) continue;

            thread.AccumulatedTime += deltaTime;
            if (thread.AccumulatedTime >= thread.CheckInterval)
            {
                thread.AccumulatedTime = 0;
                if (thread.Condition(actor, world))
                {
                    thread.OnTrigger(actor, world);
                }
            }
        }
    }
}
