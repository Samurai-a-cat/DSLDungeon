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

    // Прокси-методы
    public void AddModifier(string key, StatModifier mod) => Stats.AddModifier(key, mod);
    public void RemoveModifiersFromSource(ModifierSource source) => Stats.RemoveModifiersFromSource(source);
    public void RemoveModifiersByTag(string tag) => Stats.RemoveModifiersByTag(tag);
    public float GetValue(string key) => Stats.GetValue(key);

    public override void OnAttached(Entity owner)
    {
        base.OnAttached(owner);

        Stats.OnStatChanged += (key, value) =>
        {
            if (key == StatKeys.Constitution)
            {
                owner.GetComponent<HealthComponent>().RecalculateMaxHpFromConstitution(value);
            }
        };
    }

    public void SetupBaseStats(float str, float dex, float @int, float con)
    {
        Stats.InitializeBaseStats(new()
        {
            [StatKeys.Strength] = str,
            [StatKeys.Dexterity] = dex,
            [StatKeys.Intelligence] = @int,
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

public class EquipmentComponent : EntityComponent
{
    private readonly Dictionary<EquipmentSlot, Item> _equipped = new();

    public IReadOnlyDictionary<EquipmentSlot, Item> Equipped => _equipped;

    public bool Equip(EquipmentSlot slot, Item item)
    {
        if (_equipped.TryGetValue(slot, out _))
        {
            Unequip(slot);
        }

        _equipped[slot] = item;

        var stats = Owner.GetComponent<StatsComponent>();
        foreach (var mod in item.GetModifiers())
        {
            stats.AddModifier(mod.Key, mod.Value);
        }

        return true;
    }

    public void Unequip(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out var item)) return;

        var stats = Owner.GetComponent<StatsComponent>();
        stats.RemoveModifiersByTag(item.Name);

        _equipped.Remove(slot);
    }
}

public enum EquipmentSlot
{
    MainHand, OffHand, Head, Body, Gloves, Boots, Amulet, Ring1, Ring2, Belt
}
