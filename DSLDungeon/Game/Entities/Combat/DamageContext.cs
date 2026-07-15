using DSLDungeon.Game.Entities.Items;

namespace DSLDungeon.Game.Entities.Combat;

public class DamageContext
{
    public Entity Attacker { get; set; } = null!;
    public Entity Target { get; set; } = null!;
    public Weapon? Weapon { get; set; }

    public float BaseDamage { get; set; }
    public DamageType DamageType { get; set; } = DamageType.Physical;

    public float Distance { get; set; }
    public bool IsBackstab { get; set; }
    public bool HasHeightAdvantage { get; set; }

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

        if (attacker is Actor attackerActor)
        {
            ctx.IsBackstab = attackerActor.PositionTracker.IsBackstab(target, attacker);
            ctx.HasHeightAdvantage = attackerActor.PositionTracker.HasHeightAdvantageOver(target);

            var combat = attackerActor.Combat;
            ctx.IsImpulseActive = combat.IsImpulseActive;
            ctx.ImpulseBonus = combat.ImpulseBonus;
            ctx.ComboCount = combat.ComboCount;
        }

        return ctx;
    }
}
