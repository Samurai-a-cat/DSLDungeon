using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Core.Time;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game;

public class GameLoop
{
    private readonly WorldState _world;
    private readonly TimeChannel _gameTimeChannel = TimeSystem.CreateChannel();
    public TimeChannel GameTimeChannel => _gameTimeChannel;

    public GameLoop(WorldState world)
    {
        _world = world;
        EventPool.Initialize();
    }

    public void Update() 
    {
        TimeSystem.Update();
    
        float dt = _gameTimeChannel.Delta.Value;

        if (dt <= 0) return;

        // 1. ИИ наполняет очереди
        foreach (var actor in _world.GetAllActors()) 
        {
            if (actor.Health is { IsDead: true }) continue;

            if (actor.Queue.IsEmpty) 
            {
                RunPrototypeAI(actor); 
            }
        }

        // 2. Системы последовательно обрабатывают логику
        _world.Systems.Update(dt, _world);

        // 3. Очистка локальных очередей
        foreach (var actor in _world.GetAllActors())
        {
            actor.Queue.CleanUp(_world);
        }

        // 4. Очистка глобальной очереди мира
        _world.WorldQueue.CleanUp(_world);

        // 5. ФИНАЛЬНАЯ БЕЗОПАСНАЯ ФАЗА: Удаление сущностей из памяти и возврат ID в генератор
        _world.FinalizeDespawns();
    }

    private void RunPrototypeAI(Actor actor)
    {
        Actor? target = FindNearestEnemy(actor);
        if (target == null) return;

        int distance = actor.Position.DistanceTo(target.Position);

        if (distance > 1)
        {
            HexCoords nextStep = GetStepTowards(actor.Position, target.Position);
        
            // Рыцарь ходит очень быстро (0.4s), Орки — медленно (1.0s)
            float moveDuration = actor.Name.Contains("Рыцарь") ? 0.4f : 1.0f;

            var moveEvent = EventFactory.Create<MoveEvent>(actor.Id, e =>
            {
                e.TargetCoords = nextStep;
                e.Duration = moveDuration;
            });

            actor.Queue.Enqueue(moveEvent, _world);
            _world.AddLog($"[ИИ] {actor.Name} наметил путь на {nextStep.ToString()} (ETA: {moveDuration:0.0}s)");
        }
        else
        {
            var weapon = actor.Inventory?.EquippedWeapon;
            if (weapon != null)
            {
                var attackEvent = weapon.CreateAttackEvent(actor.Id, target.Id);
                actor.Queue.Enqueue(attackEvent, _world);
            
                _world.AddLog($"[ИИ] {actor.Name} замахнулся на {target.Name} ({weapon.Damage} урона)!");
            }
        }
    }

    #region Вспомогательные ИИ-методы
    
    private Actor? FindNearestEnemy(Actor actor)
    {
        Actor? nearest = null;
        int minDistance = int.MaxValue;

        foreach (var other in _world.GetAllActors())
        {
            if (other.Id == actor.Id) continue;
            if (other.Health?.IsDead == true) continue;
            if (other.GetType() == actor.GetType()) continue; 

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
    
    #endregion
}