using DSLDungeon.Game.Entities.Stats;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Components;

public class ImpulseComponent : EntityComponent
{
    public float BonusDamagePercent { get; set; } = 0.25f;
    public float DurationSeconds { get; set; } = 2.0f;

    private float _remainingTime;
    private bool _isActive;

    public bool IsActive => _isActive && _remainingTime > 0;

    public void Activate()
    {
        _isActive = true;
        _remainingTime = DurationSeconds;

        if (Owner.GetComponent<StatsComponent>() is { } stats)
        {
            stats.Stats.AddModifier(StatKeys.ImpulseBonus, 
                StatModifier.FinalMultiplier(BonusDamagePercent, ModifierSource.Impulse, "Physical"));
        }
    }

    public void Consume()
    {
        if (!_isActive) return;
        _isActive = false;

        if (Owner.GetComponent<StatsComponent>() is { } stats)
        {
            stats.Stats.RemoveModifiersFromSource(ModifierSource.Impulse);
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!_isActive) return;

        _remainingTime -= deltaTime;
        if (_remainingTime <= 0)
        {
            Consume();
        }
    }
}

public class BackgroundThreadComponent : EntityComponent
{
    public float CheckInterval { get; set; } = 3.0f;
    public required Func<Actor, WorldState, bool> Condition { get; set; }
    public required Action<Actor, WorldState> OnTrigger { get; set; }

    private float _timer;
    private WorldState? _world;

    public void Initialize(WorldState world) => _world = world;

    public override void OnUpdate(float deltaTime)
    {
        if (_world == null) return;
        if (Owner is not Actor actor) return;
        if (actor.GetComponent<HealthComponent>() is { IsDead: true }) return;

        _timer += deltaTime;
        if (_timer >= CheckInterval)
        {
            _timer = 0;
            if (Condition(actor, _world))
            {
                OnTrigger(actor, _world);
            }
        }
    }
}

public class PositionTrackerComponent : EntityComponent
{
    private HexCoords _lastPosition;

    public bool HasHeightAdvantageOver(Entity target)
    {
        return false;
    }

    public bool IsBackstab(Entity target, Entity attacker)
    {
        return false;
    }

    public void OnMoved(HexCoords newPosition)
    {
        _lastPosition = newPosition;

        if (Owner.GetComponent<ImpulseComponent>() is { } impulse)
        {
            impulse.Activate();
        }
    }
}

public class ComboComponent : EntityComponent
{
    public int Counter { get; private set; }
    public Entity? CurrentTarget { get; private set; }

    public float ResetTime { get; set; } = 3.0f;
    private float _timer;

    public void OnHit(Entity target)
    {
        if (CurrentTarget?.Id != target.Id)
        {
            Counter = 1;
            CurrentTarget = target;
        }
        else
        {
            Counter++;
        }

        _timer = 0;

        if (Owner.GetComponent<StatsComponent>() is { } stats)
        {
            stats.Stats.RemoveModifiersFromSource(ModifierSource.Combo);
            if (Counter > 1)
            {
                float comboMult = 1 + (Counter - 1) * 0.1f;
                stats.Stats.AddModifier(StatKeys.ComboMultiplier,
                    StatModifier.More(comboMult, ModifierSource.Combo));
            }
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Counter <= 0) return;

        _timer += deltaTime;
        if (_timer >= ResetTime)
        {
            Counter = 0;
            CurrentTarget = null;

            if (Owner.GetComponent<StatsComponent>() is { } stats)
            {
                stats.Stats.RemoveModifiersFromSource(ModifierSource.Combo);
            }
        }
    }
}

public class SimpleAIComponent : EntityComponent
{
    public override void OnUpdate(float deltaTime)
    {
    }
}
