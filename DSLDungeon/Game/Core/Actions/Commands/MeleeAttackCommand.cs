using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

[PoolConfig(10)]
public class MeleeAttackCommand : ActionCommand
{
    // _world удален! Команда больше не хранит ссылку на мир.
    protected EntityId _targetId;
    protected int _damage;

    // ReSharper disable once EmptyConstructor
    public MeleeAttackCommand() { }

    // WorldState убран из параметров
    public void Initialize(EntityId owner, EntityId targetId, int damage, float duration)
    {
        ResetBase(owner, duration);
        _targetId = targetId;
        _damage = damage;
    }

    public override void Reset()
    {
        base.Reset();
        _targetId = default;
        _damage = 0;
    }

    public override void OnStart(WorldState world)
    {
        if (!world.TryGetEntity(Owner, out var attacker) || attacker.Health?.IsDead == true)
        {
            Console.WriteLine("[MeleeAttack] Cancel: Attacker is null or dead");
            Cancel();
            return;
        }
        if (!world.TryGetEntity(_targetId, out var target) || target.Health == null || target.Health.IsDead)
        {
            Console.WriteLine("[MeleeAttack] Cancel: Target is null or dead");
            Cancel();
            return;
        }

        var distance = attacker.Position.DistanceTo(target.Position);
        
        // Используем допуск для float, так как строгое равенство == 1 опасно
        if (distance > 1.2f) 
        {
            Console.WriteLine("[MeleeAttack] Cancel: Distance is not 1!");
            Cancel();
        }
    }

    public override void OnUpdate(float deltaTime, WorldState world) { }

    public override void OnFinish(WorldState world)
    {
        if (world.TryGetEntity(Owner, out var attacker) && 
            world.TryGetEntity(_targetId, out var target))
        {
            if (attacker.Health?.IsDead != true && target.Health != null && !target.Health.IsDead)
            {
                target.Health.ModifyHp(-_damage);
            }
        }
    }

    public override void OnCancel(WorldState world) { }
}