using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(10)]
public class MoveEvent : QueueEvent
{
    public override int Priority => 5; // Низкий приоритет

    public HexCoords TargetCoords { get; set; }
    public float Duration { get; set; }
    public float ElapsedTime { get; set; }

    public override void Reset()
    {
        base.Reset();
        TargetCoords = default;
        Duration = 0f;
        ElapsedTime = 0f;
    }
}

public class MovementSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            var queue = actor.Queue;
            var activeEvent = queue.GetActiveEvent();

            if (activeEvent is MoveEvent moveEvent)
            {
                if (moveEvent.Status == EventStatus.Pending)
                {
                    moveEvent.Status = EventStatus.Executing;
                }

                moveEvent.ElapsedTime += deltaTime;

                if (moveEvent.ElapsedTime >= moveEvent.Duration)
                {
                    // Изменяем позицию актора в конце перемещения
                    actor.Position = moveEvent.TargetCoords;
                    moveEvent.Status = EventStatus.Completed;
                }
            }
        }
    }
}