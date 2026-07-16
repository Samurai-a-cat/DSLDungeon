using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Entities.Stats;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.DSL;

/// <summary>
/// Безопасный API, доступный скрипту игрока.
/// </summary>
public class DslContext
{
    private readonly Actor _actor;
    private readonly WorldState _world;

    public DslContext(Actor actor, WorldState world)
    {
        _actor = actor;
        _world = world;
    }

    // --- Состояние актора ---
    public string Name => _actor.Name;
    public int Hp => _actor.Health.CurrentHp;
    public int MaxHp => _actor.Health.MaxHp;
    public (int Q, int R) Position => (_actor.Position.Q, _actor.Position.R);
    public float Strength => _actor.Stats.GetValue(StatKey.Strength);
    public float Dexterity => _actor.Stats.GetValue(StatKey.Dexterity);
    public float Intelligence => _actor.Stats.GetValue(StatKey.Intelligence);
    public float Constitution => _actor.Stats.GetValue(StatKey.Constitution);

    // --- Действия ---

    /// <summary>
    /// Двинуться на соседнюю клетку (расстояние ровно 1).
    /// </summary>
    public void Move(int q, int r)
    {
        var target = new HexCoords(q, r);
        int dist = _actor.Position.DistanceTo(target);
        if (dist > 1)
        {
            _world.AddLog($"[DSL] {Name}: клетка ({q},{r}) слишком далеко (расстояние {dist}).");
            return;
        }

        if (!_world.Map.TryGetTile(target, out var tile) || !tile.IsPassable)
        {
            _world.AddLog($"[DSL] {Name}: клетка ({q},{r}) непроходима.");
            return;
        }

        var occupant = _world.GetEntityAt(target);
        if (occupant != null && occupant.Id != _actor.Id)
        {
            _world.AddLog($"[DSL] {Name}: клетка ({q},{r}) занята {occupant.Name}.");
            return;
        }

        var ev = EventPool.Get<MoveEvent>();
        ev.Owner = _actor.Id;
        ev.TargetCoords = target;
        ev.Duration = 0.5f / Math.Max(_actor.Stats.GetValue(StatKey.MoveSpeed), 0.1f);

        _actor.Queue.Enqueue(ev, _world);
        _world.AddLog($"[DSL] {Name} → движение на ({q},{r}).");
    }

    /// <summary>
    /// Сделать один шаг в сторону целевой клетки (pathfinding).
    /// </summary>
    public void MoveTowards(int q, int r)
    {
        var target = new HexCoords(q, r);
        var step = GetBestStepTowards(target);

        if (step == _actor.Position)
        {
            _world.AddLog($"[DSL] {Name}: не удалось найти шаг к ({q},{r}).");
            return;
        }

        Move(step.Q, step.R);
    }

    public void Attack(string targetName)
    {
        var target = FindActorByName(targetName);
        if (target == null)
        {
            _world.AddLog($"[DSL] {Name}: цель \"{targetName}\" не найдена.");
            return;
        }

        int dist = _actor.Position.DistanceTo(target.Position);
        if (dist > 1)
        {
            _world.AddLog($"[DSL] {Name}: {targetName} слишком далеко (расстояние {dist}).");
            return;
        }

        Weapon? weapon = null;
        if (_actor.TryGetComponent<EquipmentComponent>(out var eq))
            weapon = eq.Equipped.GetValueOrDefault(EquipmentSlot.MainHand) as Weapon;

        if (weapon == null)
        {
            _world.AddLog($"[DSL] {Name}: нет оружия в руках!");
            return;
        }

        var ev = weapon.CreateAttackEvent(_actor.Id, target.Id);
        _actor.Queue.Enqueue(ev, _world);
        _world.AddLog($"[DSL] {Name} → атака {targetName}.");
    }

    public void Wait(float seconds)
    {
        _world.AddLog($"[DSL] {Name} ждёт {seconds:F1}с...");
    }

    // --- Запросы ---

    public string? FindNearestEnemy()
    {
        Actor? nearest = null;
        int minDist = int.MaxValue;

        foreach (var other in _world.GetAllActors())
        {
            if (other.Id == _actor.Id) continue;
            if (other.Health.IsDead) continue;
            if (IsAlly(other)) continue;

            int dist = _actor.Position.DistanceTo(other.Position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = other;
            }
        }

        return nearest?.Name;
    }

    public DslEntityInfo? FindNearestEnemyInfo()
    {
        Actor? nearest = null;
        int minDist = int.MaxValue;

        foreach (var other in _world.GetAllActors())
        {
            if (other.Id == _actor.Id) continue;
            if (other.Health.IsDead) continue;
            if (IsAlly(other)) continue;

            int dist = _actor.Position.DistanceTo(other.Position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = other;
            }
        }

        if (nearest == null) return null;

        return new DslEntityInfo(
            nearest.Name,
            nearest.Health.CurrentHp,
            nearest.Health.MaxHp,
            nearest.Position.Q,
            nearest.Position.R,
            true
        );
    }

    public string? FindNearestAlly()
    {
        Actor? nearest = null;
        int minDist = int.MaxValue;

        foreach (var other in _world.GetAllActors())
        {
            if (other.Id == _actor.Id) continue;
            if (other.Health.IsDead) continue;
            if (!IsAlly(other)) continue;

            int dist = _actor.Position.DistanceTo(other.Position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = other;
            }
        }

        return nearest?.Name;
    }

    public List<DslEntityInfo> GetVisibleEnemies()
    {
        var result = new List<DslEntityInfo>();
        foreach (var other in _world.GetAllActors())
        {
            if (other.Id == _actor.Id) continue;
            if (other.Health.IsDead) continue;
            if (IsAlly(other)) continue;

            result.Add(new DslEntityInfo(
                other.Name,
                other.Health.CurrentHp,
                other.Health.MaxHp,
                other.Position.Q,
                other.Position.R,
                true
            ));
        }
        return result;
    }

    public List<DslEntityInfo> GetVisibleAllies()
    {
        var result = new List<DslEntityInfo>();
        foreach (var other in _world.GetAllActors())
        {
            if (other.Id == _actor.Id) continue;
            if (other.Health.IsDead) continue;
            if (!IsAlly(other)) continue;

            result.Add(new DslEntityInfo(
                other.Name,
                other.Health.CurrentHp,
                other.Health.MaxHp,
                other.Position.Q,
                other.Position.R,
                false
            ));
        }
        return result;
    }

    public DslEntityInfo? GetSelf()
    {
        return new DslEntityInfo(
            _actor.Name,
            _actor.Health.CurrentHp,
            _actor.Health.MaxHp,
            _actor.Position.Q,
            _actor.Position.R,
            false
        );
    }

    public int DistanceTo(int q, int r)
    {
        return _actor.Position.DistanceTo(new HexCoords(q, r));
    }

    // --- Вспомогательное ---

    private bool IsAlly(Actor other)
    {
        bool iAmHero = _actor.HasComponent<DslAiComponent>();
        bool otherIsHero = other.HasComponent<DslAiComponent>();
        return iAmHero == otherIsHero;
    }

    private Actor? FindActorByName(string name)
    {
        foreach (var actor in _world.GetAllActors())
        {
            if (actor.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !actor.Health.IsDead)
                return actor;
        }
        return null;
    }

    private HexCoords GetBestStepTowards(HexCoords target)
    {
        HexCoords bestStep = _actor.Position;
        int minDistance = _actor.Position.DistanceTo(target);

        for (int i = 0; i < 6; i++)
        {
            HexCoords neighbor = _actor.Position.GetNeighbor(i);

            if (_world.Map.TryGetTile(neighbor, out var tile) && tile.IsPassable)
            {
                if (_world.GetEntityAt(neighbor) != null) continue;

                int dist = neighbor.DistanceTo(target);
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
