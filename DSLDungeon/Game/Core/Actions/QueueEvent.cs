using System;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public interface IQueueEvent
{
    Guid Id { get; }
    int Priority { get; }
    EventStatus Status { get; set; }
    EntityId Owner { get; set; }
    void Reset();
}

public abstract class QueueEvent : IQueueEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public abstract int Priority { get; }
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public EntityId Owner { get; set; }

    public virtual void Reset()
    {
        Id = Guid.NewGuid();
        Status = EventStatus.Pending;
        Owner = EntityId.None;
    }
}