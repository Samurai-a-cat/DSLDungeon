using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Entities.Stats;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Components;

public class SimpleAIComponent : EntityComponent
{
    public override void OnUpdate(float deltaTime)
    {
        if (Owner is not Actor actor) return;
        if (actor.World == null) return;
        if (actor.Health.IsDead) return;

        // Прототипный ИИ: если очередь пуста — идём к ближайшему врагу и атакуем
        if (actor.Queue.IsEmpty)
        {
            RunPrototypeAI(actor);
        }
    }

    private void RunPrototypeAI(Actor actor)
    {
        var world = actor.World!;
        Actor? target = FindNearestEnemy(actor, world);
        if (target == null) return;

        int distance = actor.Position.DistanceTo(target.Position);

        world.AddLog($"[ИИ] {actor.Name} думает... (HP: {actor.Health.CurrentHp}/{actor.Health.MaxHp})");

        if (distance > 1)
        {
            HexCoords nextStep = GetStepTowards(actor.Position, target.Position, world);
            float moveDuration = actor.Stats.GetValue(StatKey.MoveSpeed) > 1.2f ? 0.4f : 0.6f;

            var moveEvent = EventPool.Get<MoveEvent>();
            moveEvent.Owner = actor.Id;
            moveEvent.TargetCoords = nextStep;
            moveEvent.Duration = moveDuration;

            actor.Queue.Enqueue(moveEvent, world);
        }
        else
        {
            Weapon? weapon = null;
            if (actor.TryGetComponent<EquipmentComponent>(out var eq))
                weapon = eq.Equipped.GetValueOrDefault(EquipmentSlot.MainHand) as Weapon;

            if (weapon != null)
            {
                var attackEvent = weapon.CreateAttackEvent(actor.Id, target.Id);
                actor.Queue.Enqueue(attackEvent, world);
            }
        }
    }

    private Actor? FindNearestEnemy(Actor actor, WorldState world)
    {
        Actor? nearest = null;
        int minDistance = int.MaxValue;

        foreach (var other in world.GetAllActors())
        {
            if (other.Id == actor.Id) continue;
            if (other.Health.IsDead) continue;

            bool isEnemy = actor.HasComponent<DslAiComponent>()
                ? !other.HasComponent<DslAiComponent>()  // Герой → орки
                : other.HasComponent<DslAiComponent>();   // Орк → герои

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

    private HexCoords GetStepTowards(HexCoords from, HexCoords to, WorldState world)
    {
        HexCoords bestStep = from;
        int minDistance = from.DistanceTo(to);

        for (int i = 0; i < 6; i++)
        {
            HexCoords neighbor = from.GetNeighbor(i);

            if (world.Map.TryGetTile(neighbor, out var tile) && tile.IsPassable)
            {
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
}
