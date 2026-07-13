namespace DSLDungeon.Game.Entities.Components;

/// <summary>
/// Единая точка правды для всех временных боевых состояний.
/// Тупо данные: таймеры, счётчики, флаги. Логика в CombatStateProcess.
/// </summary>
public class CombatStateComponent : EntityComponent
{
    public float ImpulseBonus { get; set; }
    public float ImpulseDuration { get; set; }

    public bool IsImpulseActive { get; private set; }
    public float ImpulseTimer { get; private set; }

    public void ActivateImpulse(float bonus, float duration)
    {
        IsImpulseActive = true;
        ImpulseBonus = bonus;
        ImpulseTimer = duration;
    }

    public void ConsumeImpulse()
    {
        IsImpulseActive = false;
        ImpulseBonus = 0;
        ImpulseTimer = 0;
    }

    public void TickImpulse(float dt)
    {
        if (!IsImpulseActive) return;
        ImpulseTimer -= dt;
        if (ImpulseTimer <= 0)
        {
            ConsumeImpulse();
        }
    }

    public int ComboCount { get; private set; }
    public Entity? ComboTarget { get; private set; }
    public float ComboTimer { get; private set; }
    public float ComboResetTime { get; set; } = 3.0f;

    public void OnHit(Entity target)
    {
        if (ComboTarget?.Id != target.Id)
        {
            ComboCount = 1;
            ComboTarget = target;
        }
        else
        {
            ComboCount++;
        }
        ComboTimer = 0;
    }

    public void TickCombo(float dt)
    {
        if (ComboCount <= 0) return;
        ComboTimer += dt;
        if (ComboTimer >= ComboResetTime)
        {
            ComboCount = 0;
            ComboTarget = null;
        }
    }

    public bool IsBackstab { get; set; }
    public bool HasHeightAdvantage { get; set; }

    public void Reset()
    {
        ConsumeImpulse();
        ComboCount = 0;
        ComboTarget = null;
        ComboTimer = 0;
        IsBackstab = false;
        HasHeightAdvantage = false;
    }
}
