using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Components;

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