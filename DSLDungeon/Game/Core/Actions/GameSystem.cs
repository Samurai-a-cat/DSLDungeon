using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public interface IGameSystem
{
    void Update(float deltaTime, WorldState world);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SystemOrderAttribute : Attribute
{
    public int Order { get; }

    public SystemOrderAttribute(int order)
    {
        Order = order;
    }
}

public interface IEntityTrackingSystem : IGameSystem
{
    void Register(EntityId id);
    void Unregister(EntityId id);
}

public abstract class GameSystem<TEvent> : IEntityTrackingSystem
    where TEvent : class, IQueueEvent
{
    protected readonly HashSet<EntityId> ActiveEntities = new();

    public void Register(EntityId id) => ActiveEntities.Add(id);
    public void Unregister(EntityId id) => ActiveEntities.Remove(id);

    public virtual void Update(float deltaTime, WorldState world)
    {
        foreach (var id in ActiveEntities)
        {
            if (world.TryGetEntity(id, out var entity) && entity is Actor actor)
            {
                if (actor.Queue.GetActiveEvent() is TEvent typedEvent)
                {
                    if (typedEvent.Status == EventStatus.Pending)
                    {
                        typedEvent.Status = EventStatus.Executing;
                        OnStart(actor, typedEvent, world);
                    }

                    if (typedEvent.Status == EventStatus.Executing)
                    {
                        OnUpdate(deltaTime, actor, typedEvent, world);
                    }
                }
            }
        }
    }

    protected virtual void OnStart(Actor actor, TEvent ev, WorldState world) { }
    protected abstract void OnUpdate(float deltaTime, Actor actor, TEvent ev, WorldState world);
}
