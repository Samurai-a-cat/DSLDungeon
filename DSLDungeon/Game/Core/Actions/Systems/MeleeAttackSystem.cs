using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(10)]
public class MeleeAttackEvent : QueueEvent
{
    public override int Priority => 3; // Средний приоритет

    public EntityId TargetId { get; set; }
    public int Damage { get; set; }
    public float Duration { get; set; }
    public float ElapsedTime { get; set; }

    public override void Reset()
    {
        base.Reset();
        TargetId = default;
        Damage = 0;
        Duration = 0f;
        ElapsedTime = 0f;
    }
}

public class MeleeAttackSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            var queue = actor.Queue;
            var activeEvent = queue.GetActiveEvent();

            if (activeEvent is MeleeAttackEvent attackEvent)
            {
                if (attackEvent.Status == EventStatus.Pending)
                {
                    if (!world.TryGetEntity(attackEvent.TargetId, out var target) || target.Health?.IsDead == true)
                    {
                        attackEvent.Status = EventStatus.Cancelled;
                        continue;
                    }

                    float distance = actor.Position.DistanceTo(target.Position);
                    if (distance > 1.2f)
                    {
                        Console.WriteLine($"[MeleeAttackSystem] Distance too big: {distance}. Cancelled.");
                        attackEvent.Status = EventStatus.Cancelled;
                        continue;
                    }

                    attackEvent.Status = EventStatus.Executing;
                }

                attackEvent.ElapsedTime += deltaTime;

                if (attackEvent.ElapsedTime >= attackEvent.Duration)
                {
                    if (world.TryGetEntity(attackEvent.TargetId, out var target))
                    {
                        if (actor.Health?.IsDead != true && target.Health != null && !target.Health.IsDead)
                        {
                            target.Health.ModifyHp(-attackEvent.Damage);

                            // Интеграция со смертью: если цель умерла и является Актором, сразу вешаем на неё событие смерти
                            if (target is Actor targetActor && targetActor.Health?.IsDead == true)
                            {
                                var dieEvent = EventFactory.Create<DieEvent>(targetActor.Id, _ => {});
                                targetActor.Queue.Enqueue(dieEvent);
                            }
                        }
                    }

                    attackEvent.Status = EventStatus.Completed;
                }
            }
        }
    }
}