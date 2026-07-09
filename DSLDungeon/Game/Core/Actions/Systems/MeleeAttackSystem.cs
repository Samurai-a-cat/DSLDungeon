using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Particles;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(10)]
public class MeleeAttackEvent : QueueEvent<MeleeAttackSystem>
{
    public override int Priority => 3;

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
[SystemOrder(30)]
public class MeleeAttackSystem : GameSystem<MeleeAttackEvent>, IGameSystem
{
    protected override void OnStart(Actor actor, MeleeAttackEvent ev, WorldState world)
    {
        if (!world.TryGetEntity(ev.TargetId, out var target) || target.Health?.IsDead == true)
        {
            ev.Status = EventStatus.Cancelled;
            return;
        }

        float distance = actor.Position.DistanceTo(target.Position);
        if (distance > 1.2f)
        {
            ev.Status = EventStatus.Cancelled;
        }
    }
    
    protected override void OnUpdate(float deltaTime, Actor actor, MeleeAttackEvent ev, WorldState world)
    {
        ev.ElapsedTime += deltaTime;

        if (ev.ElapsedTime >= ev.Duration)
        {
            if (world.TryGetEntity(ev.TargetId, out var target))
            {
                if (actor.Health?.IsDead != true && target.Health is { IsDead: false })
                {
                    target.Health.ModifyHp(-ev.Damage);

                    // Спавним триггер всплывающего урона на координатах жертвы
                    world.PendingDamageTriggers.Add(new VisualDamageTrigger
                    {
                        Coords = target.Position,
                        Text = $"-{ev.Damage}",
                        Type = "Damage"
                    });

                    if (target is Actor { Health.IsDead: true } targetActor)
                    {
                        var dieEvent = EventPool.Get<DieEvent>();
                        dieEvent.Owner = targetActor.Id;
    
                        targetActor.Queue.Enqueue(dieEvent, world);
                    }
                }
            }

            ev.Status = EventStatus.Completed;
        }
    }
}