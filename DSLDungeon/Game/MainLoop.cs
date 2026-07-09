using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Core.Time;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game;

public class GameLoop
{
    private readonly WorldState _world;
    
    // 1. Создаем локальную систему времени для этого цикла
    private readonly TimeSystem _timeSystem = new();
    private readonly TimeChannel _gameTimeChannel;

    public TimeChannel GameTimeChannel => _gameTimeChannel;
    
    // Предоставляем доступ к системе времени (например, для GameService)
    public TimeSystem Time => _timeSystem;

    public GameLoop(WorldState world)
    {
        _world = world;
        
        // 2. Регистрируем игровой канал времени из локальной системы
        _gameTimeChannel = _timeSystem.CreateChannel();
        
        EventPool.Initialize();
    }

    public void Update() 
    {
        // 3. Обновляем локальную систему времени
        _timeSystem.Update();
    
        float dt = _gameTimeChannel.Delta.Value;

        if (dt <= 0) return;

        // ИИ наполняет очереди
        foreach (var actor in _world.GetAllActors()) 
        {
            if (actor.Health is { IsDead: true }) continue;

            if (actor.Queue.IsEmpty) 
            {
                RunPrototypeAI(actor); 
            }
        }

        // Системы последовательно обрабатывают логику
        _world.Systems.Update(dt, _world);

        // Очистка локальных очередей
        foreach (var actor in _world.GetAllActors())
        {
            actor.Queue.CleanUp(_world);
        }

        // Очистка глобальной очереди мира
        _world.WorldQueue.CleanUp(_world);

        // Удаление сущностей из памяти
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
            float moveDuration = actor.Name.Contains("Рыцарь") ? 0.4f : 1.0f;
            
            var moveEvent = EventPool.Get<MoveEvent>();
            moveEvent.Owner = actor.Id;
            moveEvent.TargetCoords = nextStep;
            moveEvent.Duration = moveDuration;

            actor.Queue.Enqueue(moveEvent, _world);
            _world.AddLog($"[ИИ] {actor.Name} наметил путь на {nextStep.ToString()} (ETA: {moveDuration:0.0}s)");
        }
        else
        {
            var weapon = actor.Inventory?.EquippedWeapon;
            if (weapon != null)
            {
                // Оружие создает событие атаки напрямую из пула (без лямбд)
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