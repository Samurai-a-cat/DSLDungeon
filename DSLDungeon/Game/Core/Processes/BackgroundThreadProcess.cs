using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Core.Processes;

/// <summary>
/// Фоновый процесс: периодически проверяет условия фоновых потоков (магический интеллект).
/// </summary>
[SystemOrder(15)]
public class BackgroundThreadProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            var thread = actor.GetComponent<BackgroundThreadData>();
            if (actor.GetComponent<HealthComponent>() is { IsDead: true }) continue;

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
