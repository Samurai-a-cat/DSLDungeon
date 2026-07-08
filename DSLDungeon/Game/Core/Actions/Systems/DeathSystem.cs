using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(5)]
public class DieEvent : QueueEvent
{
    public override int Priority => 1; // Наивысший приоритет
}

public class DeathSystem
{
    public void Update(WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            var queue = actor.Queue;
            var activeEvent = queue.GetActiveEvent();

            if (activeEvent is DieEvent dieEvent)
            {
                if (dieEvent.Status == EventStatus.Pending)
                {
                    dieEvent.Status = EventStatus.Executing;
                }

                // Прерываем все остальные задачи актора
                queue.ClearExcept(dieEvent);

                // Завершаем событие
                dieEvent.Status = EventStatus.Completed;
            }
        }
    }
}