using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public interface IAbilityEvent
{
    string AbilityId { get; }
    float CooldownSeconds { get; }
    float CastTime { get; }

    bool Validate(Actor actor, WorldState world);
    void PayCost(Actor actor, WorldState world);
}

/// <summary>
/// Событие-способность: персонаж должен иметь право её запустить.
/// Имеет откат, стоимость, каст-тайм.
/// </summary>
public abstract class AbilityEvent<TSystem> : QueueEvent<TSystem>, IAbilityEvent
    where TSystem : class, IEntityTrackingSystem
{
    public abstract string AbilityId { get; }
    public abstract float CooldownSeconds { get; }
    public virtual float CastTime => 0f;

    public virtual bool Validate(Actor actor, WorldState world) => true;
    public virtual void PayCost(Actor actor, WorldState world) { }
}
