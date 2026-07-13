using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Components;

public class HealthComponent : EntityComponent
{
    public int MaxHp
    {
        get
        {
            var stats = Owner.GetComponent<StatsComponent>();
            return (int)(stats.GetValue(StatKeys.Constitution) * 10);
        }
    }

    public int CurrentHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    public int RegenRate { get; set; } = 1;
    public event Action? OnDeath;
    public event Action<int, int>? OnHpChanged;

    public void Initialize(int baseMaxHp)
    {
        CurrentHp = baseMaxHp;
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    public void RecalculateMaxHpFromConstitution(float conValue)
    {
        int newMax = (int)(conValue * 10);
        if (CurrentHp > newMax)
            CurrentHp = newMax;
        OnHpChanged?.Invoke(CurrentHp, newMax);
    }

    public void ModifyHp(int amount)
    {
        if (IsDead) return;
        int oldHp = CurrentHp;
        CurrentHp = Math.Clamp(CurrentHp + amount, 0, MaxHp);

        if (oldHp != CurrentHp)
        {
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public void SetMaxHpDirectly(int maxHp)
    {
        CurrentHp = Math.Min(CurrentHp, maxHp);
        OnHpChanged?.Invoke(CurrentHp, maxHp);
    }
}