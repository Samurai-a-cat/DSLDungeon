using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Core.Actions;

[PoolConfig(20)]
public class MoveToNeighborCommand : ActionCommand
{
    protected HexCoords _targetCoords;

    // ReSharper disable once EmptyConstructor
    public MoveToNeighborCommand() { }

    // WorldState убран из параметров
    public void Initialize(EntityId owner, HexCoords targetCoords, float duration)
    {
        ResetBase(owner, duration);
        _targetCoords = targetCoords;
    }

    public override void Reset()
    {
        base.Reset();
        _targetCoords = default;
    }

    public override void OnStart(WorldState world)
    {
        if (!world.TryGetEntity(Owner, out var entity) || entity.Health?.IsDead == true)
        {
            Cancel();
            return;
        }
        // Используем допуск для дистанции
        if (entity.Position.DistanceTo(_targetCoords) > 1.2f)
        {
            Cancel();
            return;
        }
        if (!world.Map.TryGetTile(_targetCoords, out var tile) || !tile.IsPassable)
        {
            Cancel();
            return;
        }
        if (world.GetEntityAt(_targetCoords) != null)
        {
            Cancel();
        }
    }

    public override void OnUpdate(float deltaTime, WorldState world) { }

    public override void OnFinish(WorldState world)
    {
        if (world.TryGetEntity(Owner, out var entity))
        {
            entity.Position = _targetCoords;
        }
    }

    public override void OnCancel(WorldState world) 
    { 
        if (world.GetEntityAt(_targetCoords) != null)
        {
            Cancel(); 
        }
    }
}