namespace DSLDungeon.Game.Entities.Systems;

public class HealthSystem
{
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    public event Action? OnDeath;

    public HealthSystem(int maxHp)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
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