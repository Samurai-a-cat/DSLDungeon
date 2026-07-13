using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Core.Actions;

/// <summary>
/// Базовый класс для систем, обрабатывающих способности (AbilityEvent).
/// Проверяет откат, валидирует условия, списывает стоимость, запускает откат.
/// </summary>
public abstract class AbilitySystem<TEvent> : GameSystem<TEvent>
    where TEvent : class, IAbilityEvent, IQueueEvent
{
    protected override void OnStart(Actor actor, TEvent ev, WorldState world)
    {
        var cds = actor.GetComponent<AbilityCooldownComponent>();

        // 1. Проверка отката
        if (cds.Cooldowns.TryGetValue(ev.AbilityId, out var remaining))
        {
            world.AddLog($"[Откат] {actor.Name}: {ev.AbilityId} ещё {remaining:F1}с!");
            ev.Status = EventStatus.Cancelled;
            return;
        }

        // 2. Валидация условий (дистанция, мана, экипировка и т.д.)
        if (!ev.Validate(actor, world))
        {
            world.AddLog($"[Невозможно] {actor.Name} не может использовать {ev.AbilityId}");
            ev.Status = EventStatus.Cancelled;
            return;
        }

        // 3. Списание стоимости
        ev.PayCost(actor, world);

        // 4. Запуск отката
        cds.Cooldowns.Add(ev.AbilityId, ev.CooldownSeconds);

        // 5. Каст-тайм (лог)
        if (ev.CastTime > 0)
        {
            world.AddLog($"[Каст] {actor.Name} начинает {ev.AbilityId}...");
        }

        OnAbilityStart(actor, ev, world);
    }

    protected abstract void OnAbilityStart(Actor actor, TEvent ev, WorldState world);
}
