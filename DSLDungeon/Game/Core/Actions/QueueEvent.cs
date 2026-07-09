using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public interface IQueueEvent
{
    Guid Id { get; }
    int Priority { get; }
    EventStatus Status { get; set; }
    EntityId Owner { get; set; }
    void OnEnqueue(WorldState world);
    void OnCleanUp(WorldState world);
    void OnFinish(WorldState world);
    void OnCancel(WorldState world);
    void Reset();
}

public abstract class QueueEvent : IQueueEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public abstract int Priority { get; }
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public EntityId Owner { get; set; }

    public virtual void OnEnqueue(WorldState world) { }
    public virtual void OnCleanUp(WorldState world) { }
    public virtual void OnFinish(WorldState world) { }
    public virtual void OnCancel(WorldState world) { }

    public virtual void Reset()
    {
        Id = Guid.NewGuid();
        Status = EventStatus.Pending;
        Owner = EntityId.None;
    }
}

public abstract class QueueEvent<TSystem> : QueueEvent 
    where TSystem : class, IEntityTrackingSystem
{
    public override void OnEnqueue(WorldState world)
    {
        world.Systems.Get<TSystem>().Register(Owner);
    }

    public override void OnCleanUp(WorldState world)
    {
        world.Systems.Get<TSystem>().Unregister(Owner);
    }
}