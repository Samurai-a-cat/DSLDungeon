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


public abstract class GameSystem<TEvent> : IEntityTrackingSystem, IGameSystem 
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
                // Проверяем, является ли активное событие в очереди событием нашей системы
                if (actor.Queue.GetActiveEvent() is TEvent typedEvent)
                {
                    // Автоматически переводим в статус Executing при старте
                    if (typedEvent.Status == EventStatus.Pending)
                    {
                        typedEvent.Status = EventStatus.Executing;
                        OnStart(actor, typedEvent, world);
                    }

                    // Если OnStart не отменил событие, выполняем обновление
                    if (typedEvent.Status == EventStatus.Executing)
                    {
                        OnUpdate(deltaTime, actor, typedEvent, world);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Вызывается один раз, когда событие переходит из Pending в Executing.
    /// Полезно для начальной валидации (например, проверки дистанции).
    /// </summary>
    protected virtual void OnStart(Actor actor, TEvent ev, WorldState world) { }

    /// <summary>
    /// Вызывается каждый кадр, пока событие находится в процессе выполнения.
    /// </summary>
    protected abstract void OnUpdate(float deltaTime, Actor actor, TEvent ev, WorldState world);
}