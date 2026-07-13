using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Combat;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Entities.Particles;
using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(10)]
public class MeleeAttackEvent : AbilityEvent<MeleeAttackSystem>
{
    public override string AbilityId => "melee_attack";
    public override int Priority => 3;
    public override float CooldownSeconds => 0f;
    public override float CastTime => 0f;

    public EntityId TargetId { get; set; }
    public float BaseDamage { get; set; }
    public string DamageType { get; set; } = "Physical";
    public float Duration { get; set; }
    public float ElapsedTime { get; set; }

    public DamageContext? ComputedContext { get; set; }

    public override bool Validate(Actor actor, WorldState world)
    {
        if (!world.TryGetEntity(TargetId, out var target) ||
            target.GetComponent<HealthComponent>() is not { IsDead: false })
            return false;

        float distance = actor.Position.DistanceTo(target.Position);
        if (distance > 1.5f)
            return false;

        return true;
    }

    public override void Reset()
    {
        base.Reset();
        TargetId = default;
        BaseDamage = 0;
        DamageType = "Physical";
        Duration = 0f;
        ElapsedTime = 0f;
        ComputedContext = null;
    }
}

[SystemOrder(30)]
public class MeleeAttackSystem : AbilitySystem<MeleeAttackEvent>
{
    protected override void OnAbilityStart(Actor actor, MeleeAttackEvent ev, WorldState world)
    {
        var weapon = actor.GetComponent<EquipmentComponent>().Equipped
            .GetValueOrDefault(EquipmentSlot.MainHand) as Weapon;
        var stats = actor.GetComponent<StatsComponent>().Stats;

        if (weapon != null)
        {
            float baseDmg = ev.BaseDamage;
            float str = stats.GetValue(StatKeys.Strength);
            float damageMore = stats.GetValue(StatKeys.DamageMore);
            if (damageMore <= 0) damageMore = 1f;

            float predicted = (baseDmg + str * 0.5f) * damageMore;
            world.AddLog($"[ИИ] {actor.Name} замахивается {weapon.Name} ({baseDmg:F1} базы + {str * 0.5f:F1} от силы) ≈ {predicted:F1} урона");
        }
    }

    protected override void OnUpdate(float deltaTime, Actor actor, MeleeAttackEvent ev, WorldState world)
    {
        ev.ElapsedTime += deltaTime;

        if (ev.ElapsedTime >= ev.Duration)
        {
            if (world.TryGetEntity(ev.TargetId, out var target))
            {
                var attackerHealth = actor.GetComponent<HealthComponent>();
                var targetHealth = target.GetComponent<HealthComponent>();

                if (attackerHealth is { IsDead: false } && targetHealth is { IsDead: false })
                {
                    var weapon = actor.GetComponent<EquipmentComponent>().Equipped
                        .GetValueOrDefault(EquipmentSlot.MainHand) as Weapon;

                    var ctx = DamageContext.CreateMelee(actor, target, weapon);
                    ctx.BaseDamage = ev.BaseDamage;
                    ctx.DamageType = ev.DamageType;

                    float damage = DamagePipeline.Calculate(ctx);

                    var combat = actor.GetComponent<CombatStateComponent>();
                    if (combat is { IsImpulseActive: true })
                    {
                        world.AddLog($"{actor.Name} использовал импульс)");
                        combat.ConsumeImpulse();
                    }

                    ev.ComputedContext = ctx;

                    string critTag = ctx.IsCritical ? "[КРИТ] " : "";
                    world.AddLog($"{critTag}[Удар] {actor.Name} → {target.Name}: {(int)damage} урона ({weapon?.Name ?? "без оружия"})");

                    targetHealth.ModifyHp(-(int)damage);

                    string damageText = ctx.IsCritical ? $"КРИТ! -{(int)damage}" : $"-{(int)damage}";
                    world.PendingDamageTriggers.Add(new VisualDamageTrigger(
                        target.Position,
                        damageText,
                        ctx.IsCritical ? "CritDamage" : "Damage"
                    ));

                    combat?.OnHit(target);

                    if (targetHealth.IsDead && target is Actor targetActor)
                    {
                        var dieEvent = EventPool.Get<DieEvent>();
                        dieEvent.Owner = targetActor.Id;
                        targetActor.Queue.Enqueue(dieEvent, world);

                        world.AddLog($"[Смерть] {targetActor.Name} погиб от рук {actor.Name}.");
                    }
                }
            }

            ev.Status = EventStatus.Completed;
        }
    }
}
