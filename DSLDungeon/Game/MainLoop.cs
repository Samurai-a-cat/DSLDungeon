using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Time;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game;

public class GameLoop(WorldState world)
{
    private readonly TimeChannel _gameTimeChannel = TimeSystem.CreateChannel();
    public TimeChannel GameTimeChannel => _gameTimeChannel;

    public void Update() 
    {
        TimeSystem.Update();
        
        float dt = _gameTimeChannel.Delta.Value;

        if (dt <= 0) return;

        foreach (var actor in world.GetAllActors()) 
        {
            if (actor.Health is { IsDead: true }) continue;

            if (actor.Queue.IsEmpty) 
            {
                RunPrototypeAI(actor); 
            }
            actor.Queue.Update(dt);
        }
    }

    /// <summary>
    /// Прототип ИИ, реализующий логику: "Найти врага -> Подойти -> Ударить"
    /// </summary>
    private void RunPrototypeAI(Actor actor)
    {
        Actor? target = FindNearestEnemy(actor);
        if (target == null) return;

        int distance = actor.Position.DistanceTo(target.Position);

        if (distance > 1)
        {
            HexCoords nextStep = GetStepTowards(actor.Position, target.Position);
            var cmd = ActionFactory.Create<MoveToNeighborCommand>(c => c.Initialize(actor.Id, nextStep, 0.8f));
    
            actor.Queue.Enqueue(cmd);
            Console.WriteLine($"[ИИ] {actor.Name} ({actor.Position}) идет к {target.Name} на клетку {nextStep}");
        }
        else
        {
            // Враг на соседней клетке. Бьем его!
            var weapon = actor.Inventory?.EquippedWeapon;
            if (weapon != null)
            {
                // Запрашиваем у оружия команду атаки (берет урон и скорость из оружия)
                var cmd = ActionFactory.Create<MeleeAttackCommand>(c => c.Initialize(actor.Id, target.Id, weapon.Damage, weapon.AttackSpeed));
                actor.Queue.Enqueue(cmd);
                
                Console.WriteLine($"[ИИ] {actor.Name} атакует {target.Name} мечом ({weapon.Damage} урона)!");
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

        foreach (var other in world.GetAllActors())
        {
            if (other.Id == actor.Id) continue; // Не бьем самого себя
            if (other.Health?.IsDead == true) continue; // Игнорируем мертвых

            // Для простоты прототипа: Герой бьет Орков, Орки бьют Героев
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

    /// <summary>
    /// Вычисляет соседний гекс, находящийся на кратчайшем пути к цели
    /// </summary>
    private HexCoords GetStepTowards(HexCoords from, HexCoords to)
    {
        HexCoords bestStep = from;
        int minDistance = from.DistanceTo(to);

        for (int i = 0; i < 6; i++)
        {
            HexCoords neighbor = from.GetNeighbor(i);
            
            // Проверяем проходимость клетки
            if (world.Map.TryGetTile(neighbor, out var tile) && tile.IsPassable)
            {
                // Проверяем, не занята ли клетка кем-то другим
                if (world.GetEntityAt(neighbor) != null) continue;

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