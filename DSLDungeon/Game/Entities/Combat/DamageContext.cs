using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Items;

namespace DSLDungeon.Game.Entities.Combat;

public class DamageContext
{
    public Entity Attacker { get; set; } = null!;
    public Entity Target { get; set; } = null!;
    public Weapon? Weapon { get; set; }

    public float BaseDamage { get; set; }
    public string DamageType { get; set; } = "Physical";

    public float Distance { get; set; }
    public bool IsBackstab { get; set; }
    public bool HasHeightAdvantage { get; set; }

    // Временные бонусы из CombatState
    public bool IsImpulseActive { get; set; }
    public float ImpulseBonus { get; set; }
    public int ComboCount { get; set; }

    public float FinalDamage { get; set; }
    public bool IsCritical { get; set; }
    public float LifeLeech { get; set; }

    public static DamageContext CreateMelee(Entity attacker, Entity target, Weapon? weapon)
    {
        var ctx = new DamageContext
        {
            Attacker = attacker,
            Target = target,
            Weapon = weapon,
            Distance = attacker.Position.DistanceTo(target.Position),
        };

        // Геометрия (через PositionTracker, пока заглушки)
        if (attacker.GetComponent<PositionTrackerComponent>() is { } tracker)
        {
            ctx.IsBackstab = tracker.IsBackstab(target, attacker);
        }

        if (attacker.GetComponent<PositionTrackerComponent>() is { } atkTracker &&
            target.GetComponent<PositionTrackerComponent>() is { } tgtTracker)
        {
            ctx.HasHeightAdvantage = atkTracker.HasHeightAdvantageOver(target);
        }

        // Временные бонусы из CombatState (единая точка правды)
        if (attacker.GetComponent<CombatStateComponent>() is { } combat)
        {
            ctx.IsImpulseActive = combat.IsImpulseActive;
            ctx.ImpulseBonus = combat.ImpulseBonus;
            ctx.ComboCount = combat.ComboCount;
        }

        return ctx;
    }
}