using System;
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
    
    // Глобальные системы в единственном экземпляре
    private readonly DeathSystem _deathSystem = new();
    private readonly MovementSystem _movementSystem = new();
    private readonly MeleeAttackSystem _meleeAttackSystem = new();

    public TimeChannel GameTimeChannel => _gameTimeChannel;

    public GameLoop(WorldState world)
    {
        _world = world;
        
        // Обязательная инициализация пула событий при старте игры
        EventPool.Initialize();
    }

    public void Update() 
    {
        TimeSystem.Update();
        
        float dt = _gameTimeChannel.Delta.Value;

        if (dt <= 0) return;

        // 1. ИИ наполняет очереди свободным акторам
        foreach (var actor in _world.GetAllActors()) 
        {
            if (actor.Health is { IsDead: true }) continue;

            if (actor.Queue.IsEmpty) 
            {
                RunPrototypeAI(actor); 
            }
        }

        // 2. Системы поочередно выполняют логику над событиями в очередях
        _deathSystem.Update(_world);
        _movementSystem.Update(dt, _world);
        _meleeAttackSystem.Update(dt, _world);

        // 3. Освобождение завершенных событий обратно в пул
        foreach (var actor in _world.GetAllActors())
        {
            actor.Queue.CleanUp();
        }
    }

    private void RunPrototypeAI(Actor actor)
    {
        Actor? target = FindNearestEnemy(actor);
        if (target == null) return;

        int distance = actor.Position.DistanceTo(target.Position);

        if (distance > 1)
        {
            HexCoords nextStep = GetStepTowards(actor.Position, target.Position);
            
            // Замена ActionFactory на EventFactory
            var moveEvent = EventFactory.Create<MoveEvent>(actor.Id, e =>
            {
                e.TargetCoords = nextStep;
                e.Duration = 0.8f;
            });
    
            actor.Queue.Enqueue(moveEvent);
            Console.WriteLine($"[ИИ] {actor.Name} ({actor.Position}) решил идти к {target.Name} на клетку {nextStep}");
        }
        else
        {
            var weapon = actor.Inventory?.EquippedWeapon;
            if (weapon != null)
            {
                // Запрашиваем у оружия создание события атаки
                var attackEvent = weapon.CreateAttackEvent(actor.Id, target.Id);
                actor.Queue.Enqueue(attackEvent);
                
                Console.WriteLine($"[ИИ] {actor.Name} замахнулся на {target.Name} мечом ({weapon.Damage} урона)!");
            }
            else
            {
                Console.WriteLine($"[ИИ] {actor.Name} хочет атаковать {target.Name}, но у него нет оружия!");
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