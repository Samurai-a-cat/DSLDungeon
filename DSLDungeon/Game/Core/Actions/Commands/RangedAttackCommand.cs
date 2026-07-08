using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

[PoolConfig(5)]
public class RangedAttackCommand : MeleeAttackCommand
{
    // ReSharper disable once EmptyConstructor
    public RangedAttackCommand() { }

    // Initialize, Reset, OnFinish, OnUpdate, OnCancel наследуются от MeleeAttackCommand!

    public override void OnStart(WorldState world)
    {
        // Используем параметр world вместо поля _world
        if (!world.TryGetEntity(Owner, out var attacker) || attacker.Health?.IsDead == true)
        {
            Cancel();
            return;
        }
        if (!world.TryGetEntity(_targetId, out var target) || target.Health == null || target.Health.IsDead)
        {
            Cancel();
            return;
        }

        var distance = attacker.Position.DistanceTo(target.Position);
        
        // Дальнобойная атака не может быть в упор (дистанция <= 1)
        // И добавим проверку максимальной дальности (например, > 5)
        if (distance <= 1.0f || distance > 5.0f) 
        {
            Cancel();
        }
    }
}