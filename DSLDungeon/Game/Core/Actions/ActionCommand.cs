using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public abstract class ActionCommand
{
    public EntityId Owner { get; protected set; }
    public float ActionDuration { get; protected set; }
    public float TimeRemaining { get; private set; }
    public bool IsCancelled { get; protected set; }
    public virtual bool IsUninterruptible => false;
    public bool IsFinished => TimeRemaining <= 0 || IsCancelled;
    
    private bool _returnedToPool;

    /// <summary>
    /// Явно освобождает команду и возвращает её в пул.
    /// </summary>
    public void Release()
    {
        if (_returnedToPool) return;
    
        _returnedToPool = true;
        ActionPool.ReturnInternal(this);
    }

    internal void MarkRented() => _returnedToPool = false;

    public void Cancel()
    {
        if (IsCancelled) return;
        IsCancelled = true;
    }

    protected void ResetBase(EntityId owner, float duration)
    {
        Owner = owner;
        ActionDuration = duration;
        TimeRemaining = duration;
        IsCancelled = false;
    }

    public virtual void Reset()
    {
        Owner = default;
        ActionDuration = 0;
        TimeRemaining = 0;
        IsCancelled = false;
    }

    public float Tick(float deltaTime, WorldState world)
    {
        if (IsFinished) return deltaTime;
        if (TimeRemaining <= deltaTime)
        {
            float leftover = deltaTime - TimeRemaining;
            TimeRemaining = 0;
            OnUpdate(deltaTime - leftover, world);
            return leftover;
        }
        TimeRemaining -= deltaTime;
        OnUpdate(deltaTime, world);
        return 0f;
    }

    public abstract void OnStart(WorldState world);
    public abstract void OnUpdate(float deltaTime, WorldState world);
    public abstract void OnFinish(WorldState world);
    public abstract void OnCancel(WorldState world);
}