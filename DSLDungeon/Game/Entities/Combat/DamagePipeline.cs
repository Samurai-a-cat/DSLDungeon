using System;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Combat;

public static class DamagePipeline
{
    public static float Calculate(DamageContext ctx)
    {
        var attackerStats = ctx.Attacker.GetComponent<StatsComponent>();
        StatSheet? targetStats = null;
        if (ctx.Target.TryGetComponent<StatsComponent>(out var ts)) targetStats = ts.Stats;

        float baseDamage = ctx.BaseDamage;
        if (ctx.Weapon != null)
        {
            baseDamage = ctx.Weapon.GetBaseDamage();
        }

        float str = attackerStats.GetValue(StatKey.Strength);
        float strBonus = str * 0.5f;

        float baseResult = (baseDamage + strBonus) * (1 + attackerStats.GetValue(StatKey.DamageBase) / 100);

        float addedDamage = attackerStats.GetValue(StatKey.DamageAdded);
        float addedResult = addedDamage * (1 + attackerStats.GetValue(StatKeyRegistry.Parse("dmg_added_mult")));

        float moreMult = attackerStats.GetValue(StatKey.DamageMore);
        if (moreMult <= 0) moreMult = 1f;

        if (ctx.IsImpulseActive)
        {
            moreMult *= (1 + ctx.ImpulseBonus);
        }

        if (ctx.ComboCount > 0)
        {
            float comboMult = 1 + (ctx.ComboCount - 1) * 0.1f;
            moreMult *= comboMult;
        }

        float finalMult = 1f;

        if (ctx.IsBackstab)
        {
            float backstabBonus = attackerStats.GetValue(StatKey.BackstabBonus);
            if (backstabBonus <= 0) backstabBonus = 0.3f;
            finalMult *= (1 + backstabBonus);
        }

        if (ctx.HasHeightAdvantage)
        {
            float heightBonus = attackerStats.GetValue(StatKey.HeightAdvantage);
            if (heightBonus <= 0) heightBonus = 0.15f;
            finalMult *= (1 + heightBonus);
        }

        float critChance = attackerStats.GetValue(StatKey.CritChance);
        bool isCrit = new Random().NextDouble() < critChance;
        float critMult = isCrit ? attackerStats.GetValue(StatKey.CritMultiplier) : 1f;

        float subtotal = (baseResult + addedResult) * moreMult;
        float final = subtotal * finalMult * critMult;

        if (targetStats != null)
        {
            if (ctx.DamageType == DamageType.Physical)
            {
                float armor = targetStats.GetValue(StatKey.Armor);
                float mitigation = armor / (armor + 100f);
                final *= (1 - mitigation);
            }

            float resistance = targetStats.GetValue(ctx.DamageType.GetResistanceStat());
            final *= (1 - Math.Clamp(resistance, 0, 0.9f));
        }

        ctx.FinalDamage = Math.Max(1, final);
        ctx.IsCritical = isCrit;

        return ctx.FinalDamage;
    }
}
