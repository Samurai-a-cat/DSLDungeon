using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(5)]
public class DieEvent : QueueEvent<DeathSystem>
{
    public override int Priority => 1;
}

public class DeathSystem : GameSystem<DieEvent>, IGameSystem
{
    protected override void OnUpdate(float deltaTime, Actor actor, DieEvent ev, WorldState world)
    {
        // 1. Отменяем все остальные действия в очереди актора
        actor.Queue.ClearExcept(ev);

        // 2. Регистрируем актора на удаление из мира
        world.Despawn(actor.Id);

        // 3. Завершаем событие смерти
        ev.Status = EventStatus.Completed;
    }
}