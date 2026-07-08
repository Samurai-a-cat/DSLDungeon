using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;

namespace DSLDungeon.Game.Entities.Items;

public class Weapon : Item
{
    public int Damage { get; }
    public int Range { get; }
    public float AttackSpeed { get; } 
    public bool IsRanged { get; }

    public Weapon(string name, int damage, int range, float attackSpeed, bool isRanged) : base(name)
    {
        Damage = damage;
        Range = range;
        AttackSpeed = attackSpeed;
        IsRanged = isRanged;
    }

    public IQueueEvent CreateAttackEvent(EntityId attacker, EntityId target)
    {
        // Для ближнего боя создаем MeleeAttackEvent из пула
        return EventFactory.Create<MeleeAttackEvent>(attacker, ev =>
        {
            ev.TargetId = target;
            ev.Damage = Damage;
            ev.Duration = AttackSpeed;
        });
    }
}