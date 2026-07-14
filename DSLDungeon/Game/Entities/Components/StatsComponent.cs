using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Components;

public class StatsComponent : EntityComponent
{
    public StatSheet Stats { get; } = new();

    public void AddModifier(StatKey key, StatModifier mod) => Stats.AddModifier(key, mod);
    public void RemoveModifiersFromSource(ModifierSource source) => Stats.RemoveModifiersFromSource(source);
    public void RemoveModifiersByTag(string tag) => Stats.RemoveModifiersByTag(tag);
    public float GetValue(StatKey key) => Stats.GetValue(key);

    public override void OnAttached(Entity owner)
    {
        base.OnAttached(owner);

        Stats.OnStatChanged += (key, value) =>
        {
            if (key == StatKey.Constitution)
            {
                owner.GetComponent<HealthComponent>().RecalculateMaxHpFromConstitution(value);
            }
        };
    }

    public void SetupBaseStats(float str, float dex, float @int, float con)
    {
        Stats.InitializeBaseStats(new()
        {
            [StatKey.Strength] = str,
            [StatKey.Dexterity] = dex,
            [StatKey.Intelligence] = @int,
            [StatKey.Constitution] = con,
        });

        Stats.AddModifier(StatKey.CritChance, StatModifier.Base(0.05f));
        Stats.AddModifier(StatKey.CritMultiplier, StatModifier.Base(1.5f));
        Stats.AddModifier(StatKey.AttackSpeed, StatModifier.Base(1.0f));
        Stats.AddModifier(StatKey.MoveSpeed, StatModifier.Base(1.0f));
    }
}
