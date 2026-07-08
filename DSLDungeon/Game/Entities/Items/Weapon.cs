using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;

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

    public ActionCommand CreateAttackCommand(EntityId attacker, EntityId target)
    {
        if (IsRanged)
        {
            return ActionFactory.Create<RangedAttackCommand>(cmd => 
                cmd.Initialize(attacker, target, Damage, AttackSpeed));
        }
    
        return ActionFactory.Create<MeleeAttackCommand>(cmd => 
            cmd.Initialize(attacker, target, Damage, AttackSpeed));
    }
}