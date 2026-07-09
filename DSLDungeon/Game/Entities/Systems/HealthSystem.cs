namespace DSLDungeon.Game.Entities.Systems;

public class HealthSystem
{
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    // Скорость регенерации (HP в секунду)
    public int RegenRate { get; set; } = 1; 

    public event Action? OnDeath;

    public HealthSystem(int maxHp, int regenRate = 1)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
        RegenRate = regenRate;
    }

    public void ModifyHp(int amount)
    {
        if (IsDead) return; 
        CurrentHp = System.Math.Clamp(CurrentHp + amount, 0, MaxHp);
        
        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }
}