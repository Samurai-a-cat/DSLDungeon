using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Components;

public abstract class EntityComponent
{
    public Entity Owner { get; private set; } = null!;

    public virtual void OnAttached(Entity owner) => Owner = owner;
    public virtual void OnDetached() { }
    public virtual void OnUpdate(float deltaTime) { }
}

public class StatsComponent : EntityComponent
{
    public StatSheet Stats { get; } = new();

    public override void OnAttached(Entity owner)
    {
        base.OnAttached(owner);

        Stats.OnStatChanged += (key, value) =>
        {
            if (key == StatKeys.Constitution)
            {
                if (owner.GetComponent<HealthComponent>() is { } health)
                {
                    health.RecalculateMaxHpFromConstitution(value);
                }
            }
        };
    }

    public void SetupBaseStats(float str, float dex, float int_, float con)
    {
        Stats.InitializeBaseStats(new()
        {
            [StatKeys.Strength] = str,
            [StatKeys.Dexterity] = dex,
            [StatKeys.Intelligence] = int_,
            [StatKeys.Constitution] = con,
        });

        Stats.AddModifier(StatKeys.CritChance, StatModifier.Base(0.05f));
        Stats.AddModifier(StatKeys.CritMultiplier, StatModifier.Base(1.5f));
        Stats.AddModifier(StatKeys.AttackSpeed, StatModifier.Base(1.0f));
        Stats.AddModifier(StatKeys.MoveSpeed, StatModifier.Base(1.0f));
    }
}

public class HealthComponent : EntityComponent
{
    public int MaxHp 
    { 
        get
        {
            var stats = Owner.GetComponent<StatsComponent>();
            if (stats == null) return 100;
            return (int)(stats.Stats.GetValue(StatKeys.Constitution) * 10);
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

public class EquipmentComponent : EntityComponent
{
    private readonly Dictionary<EquipmentSlot, Item> _equipped = new();

    public IReadOnlyDictionary<EquipmentSlot, Item> Equipped => _equipped;

    public bool Equip(EquipmentSlot slot, Item item)
    {
        if (_equipped.TryGetValue(slot, out var oldItem))
        {
            Unequip(slot);
        }

        _equipped[slot] = item;

        if (Owner.GetComponent<StatsComponent>() is { } stats)
        {
            foreach (var mod in item.GetModifiers())
            {
                stats.Stats.AddModifier(mod.Key, mod.Value);
            }
        }

        return true;
    }

    public void Unequip(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out var item)) return;

        if (Owner.GetComponent<StatsComponent>() is { } stats)
        {
            stats.Stats.RemoveModifiersByTag(item.Name);
        }

        _equipped.Remove(slot);
    }
}

public enum EquipmentSlot
{
    MainHand, OffHand, Head, Body, Gloves, Boots, Amulet, Ring1, Ring2, Belt
}
