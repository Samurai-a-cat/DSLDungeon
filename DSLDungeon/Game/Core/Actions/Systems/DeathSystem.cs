using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(5)]
public class DieEvent : QueueEvent<DeathSystem>
{
    public override int Priority => 1;
}

public class DeathSystem : GameSystem<DieEvent>, IEntityTrackingSystem, IGameSystem
{
    public new void Register(EntityId id) => base.Register(id);
    public new void Unregister(EntityId id) => base.Unregister(id);

    protected override void OnUpdate(float deltaTime, Actor actor, DieEvent ev, WorldState world)
    {
        actor.Queue.ClearExcept(ev);
        ev.Status = EventStatus.Completed;
    }
}
