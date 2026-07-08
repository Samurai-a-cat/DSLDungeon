using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;
/// <summary>
/// Сбрасывает пулл команд
/// </summary>
[PoolConfig(10)]
public class ClearQueueCommand : ActionCommand
{
    // ReSharper disable once EmptyConstructor
    public ClearQueueCommand() { }

    public void Initialize(EntityId owner)
    {
        // Длительность 0, так как команда должна сработать и завершиться в этот же тик
        ResetBase(owner, 0f); 
    }

    public override void OnStart(WorldState world)
    {
        if (!world.TryGetEntity(Owner, out var entity))
        {
            Cancel();
            return;
        }

        // Проверяем, является ли сущность Актером, у которого есть очередь
        if (entity is Actor actor)
        {
            actor.Queue.Clear();
        }
    }

    public override void OnUpdate(float deltaTime, WorldState world) { }
    public override void OnFinish(WorldState world) { }
    public override void OnCancel(WorldState world) { }
}