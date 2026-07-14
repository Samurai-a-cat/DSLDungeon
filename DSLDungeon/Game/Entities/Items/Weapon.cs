using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Entities.Combat;

namespace DSLDungeon.Game.Entities.Items;

public class Weapon : Item
{
    public int BaseDamage { get; }
    public int Range { get; }
    public float BaseAttackSpeed { get; } 
    public DamageType DamageType { get; }
    public bool IsRanged { get; }

    public float Quality { get; set; } = 1.0f;

    public Weapon(string name, int baseDamage, int range, float attackSpeed, 
        DamageType damageType = DamageType.Physical, bool isRanged = false) : base(name)
    {
        BaseDamage = baseDamage;
        Range = range;
        BaseAttackSpeed = attackSpeed;
        DamageType = damageType;
        IsRanged = isRanged;
    }

    public float GetBaseDamage() => BaseDamage * Quality;

    public IQueueEvent CreateAttackEvent(EntityId attacker, EntityId target)
    {
        var ev = EventPool.Get<MeleeAttackEvent>();
        ev.Owner = attacker;
        ev.TargetId = target;
        ev.BaseDamage = BaseDamage;
        ev.DamageType = DamageType;
        ev.Duration = BaseAttackSpeed;

        return ev;
    }
}
