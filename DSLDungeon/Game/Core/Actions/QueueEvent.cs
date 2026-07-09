using System.Threading;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public interface IQueueEvent
{
    long Id { get; } // Теперь идентификатор числовой
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
    private static long _globalEventId; // Глобальный счетчик идентификаторов

    public long Id { get; private set; } = Interlocked.Increment(ref _globalEventId);
    public abstract int Priority { get; }
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public EntityId Owner { get; set; }

    public virtual void OnEnqueue(WorldState world) { }
    public virtual void OnCleanUp(WorldState world) { }
    public virtual void OnFinish(WorldState world) { }
    public virtual void OnCancel(WorldState world) { }

    public virtual void Reset()
    {
        // При возврате в пул просто выдаем новый быстрый ID
        Id = Interlocked.Increment(ref _globalEventId);
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