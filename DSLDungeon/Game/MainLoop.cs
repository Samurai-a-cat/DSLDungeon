using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Core.Time;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game;

public class GameLoop
{
    private readonly WorldState _world;
    private readonly TimeSystem _timeSystem = new();
    private readonly TimeChannel _gameTimeChannel;

    public TimeChannel GameTimeChannel => _gameTimeChannel;
    public TimeSystem Time => _timeSystem;

    public GameLoop(WorldState world)
    {
        _world = world;
        _gameTimeChannel = _timeSystem.CreateChannel();
        EventPool.Initialize();
    }

    public void Update() 
    {
        _timeSystem.Update();
        float dt = _gameTimeChannel.Delta.Value;

        if (dt <= 0) return;

        foreach (var actor in _world.GetAllActors()) 
        {
            if (actor.Health.IsDead) continue;

            if (actor.Queue.IsEmpty) 
            {
                RunPrototypeAI(actor); 
            }
        }

        _world.Systems.Update(dt, _world);

        foreach (var actor in _world.GetAllActors())
        {
            actor.Queue.CleanUp(_world);
        }

        _world.WorldQueue.CleanUp(_world);
        _world.FinalizeDespawns();
    }

    private void RunPrototypeAI(Actor actor)
    {
        Actor? target = FindNearestEnemy(actor);
        if (target == null) return;

        int distance = actor.Position.DistanceTo(target.Position);

        _world.AddLog($"[ИИ] {actor.Name} думает... (HP: {actor.Health.CurrentHp}/{actor.Health.MaxHp})");

        if (distance > 1)
        {
            HexCoords nextStep = GetStepTowards(actor.Position, target.Position);
            float moveDuration = actor.Name.Contains("Рыцарь") ? 0.4f : 0.6f;

            var moveEvent = EventPool.Get<MoveEvent>();
            moveEvent.Owner = actor.Id;
            moveEvent.TargetCoords = nextStep;
            moveEvent.Duration = moveDuration;

            actor.Queue.Enqueue(moveEvent, _world);
        }
        else
        {
            var weapon = actor.TryGetComponent<EquipmentComponent>()?.Equipped
                .GetValueOrDefault(EquipmentSlot.MainHand) as Weapon;

            if (weapon != null)
            {
                var attackEvent = weapon.CreateAttackEvent(actor.Id, target.Id);
                actor.Queue.Enqueue(attackEvent, _world);
            }
        }
    }

    private Actor? FindNearestEnemy(Actor actor)
    {
        Actor? nearest = null;
        int minDistance = int.MaxValue;

        foreach (var other in _world.GetAllActors())
        {
            if (other.Id == actor.Id) continue;
            if (other.Health.IsDead) continue;

            bool isEnemy = actor.Name.Contains("Рыцарь") ? other.Name.Contains("Орк") : other.Name.Contains("Рыцарь");
            if (!isEnemy) continue;

            int dist = actor.Position.DistanceTo(other.Position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = other;
            }
        }

        return nearest;
    }

    private HexCoords GetStepTowards(HexCoords from, HexCoords to)
    {
        HexCoords bestStep = from;
        int minDistance = from.DistanceTo(to);

        for (int i = 0; i < 6; i++)
        {
            HexCoords neighbor = from.GetNeighbor(i);

            if (_world.Map.TryGetTile(neighbor, out var tile) && tile.IsPassable)
            {
                if (_world.GetEntityAt(neighbor) != null) continue;

                int dist = neighbor.DistanceTo(to);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestStep = neighbor;
                }
            }
        }

        return bestStep;
    }
}
