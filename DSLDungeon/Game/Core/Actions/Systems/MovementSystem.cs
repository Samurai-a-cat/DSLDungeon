using System;
using System.Collections.Generic;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Core.Actions.Systems;

// --- СОБЫТИЕ ПЕРЕМЕЩЕНИЯ (Без изменений) ---
[PoolConfig(10)]
public class MoveEvent : QueueEvent<MovementSystem>
{
    public override int Priority => 5;

    public HexCoords TargetCoords { get; set; }
    public float Duration { get; set; }
    public float ElapsedTime { get; set; }

    public override void OnCleanUp(WorldState world)
    {
        base.OnCleanUp(world);
        world.Systems.Get<MovementSystem>().RemoveReservation(Owner, TargetCoords);
    }

    public override void OnFinish(WorldState world)
    {
        if (world.TryGetEntity(Owner, out var entity))
        {
            world.AddLog($"[Движение] {entity.Name} успешно прибыл на клетку {TargetCoords.ToString()}.");
        }
    }

    public override void OnCancel(WorldState world)
    {
        if (world.TryGetEntity(Owner, out var entity))
        {
            world.AddLog($"[Движение] Движение {entity.Name} на клетку {TargetCoords.ToString()} было прервано.");
        }
    }

    public override void Reset()
    {
        base.Reset();
        TargetCoords = default;
        Duration = 0f;
        ElapsedTime = 0f;
    }
}

// --- СИСТЕМА ДВИЖЕНИЯ (Оптимизированная) ---
public class MovementSystem : GameSystem<MoveEvent>, IEntityTrackingSystem, IGameSystem
{
    // 1. Делаем MoveReservation структурой. Занимает всего 12 байт!
    private struct MoveReservation
    {
        public EntityId ActiveActor { get; set; }
        public float Eta { get; set; }
    }

    // Реестр броней хранит структуры напрямую в бакетах словаря (zero-allocation)
    private readonly Dictionary<HexCoords, MoveReservation> _tileReservations = new();

    public new void Register(EntityId id) => base.Register(id);
    public new void Unregister(EntityId id) => base.Unregister(id);

    protected override void OnStart(Actor actor, MoveEvent ev, WorldState world)
    {
        if (world.Map.TryGetTile(ev.TargetCoords, out var tile) && !tile.IsPassable)
        {
            world.AddLog($"[Препятствие] {actor.Name} не может наступить на непроходимый тайл {ev.TargetCoords.ToString()}.");
            ev.Status = EventStatus.Cancelled;
            return;
        }

        var physicalOccupant = world.GetEntityAt(ev.TargetCoords);
        if (physicalOccupant != null && physicalOccupant.Id != actor.Id)
        {
            if (physicalOccupant is Actor occupantActor && occupantActor.Queue.GetActiveEvent() is MoveEvent occupantMove)
            {
                float occupantTimeLeft = occupantMove.Duration - occupantMove.ElapsedTime;
                if (ev.Duration < occupantTimeLeft)
                {
                    world.AddLog($"[Конфликт] {actor.Name} прервал движение: клетка {ev.TargetCoords.ToString()} еще занята уходящим {occupantActor.Name}.");
                    ev.Status = EventStatus.Cancelled;
                    return;
                }
            }
            else
            {
                world.AddLog($"[Конфликт] {actor.Name} не может пойти на {ev.TargetCoords.ToString()}: клетка занята {physicalOccupant.Name}.");
                ev.Status = EventStatus.Cancelled;
                return;
            }
        }

        if (_tileReservations.TryGetValue(ev.TargetCoords, out var reservation))
        {
            if (ev.Duration < reservation.Eta)
            {
                if (world.TryGetEntity(reservation.ActiveActor, out var slowerEntity) && slowerEntity is Actor slowerActor)
                {
                    if (slowerActor.Queue.GetActiveEvent() is MoveEvent slowerMove)
                    {
                        slowerMove.Status = EventStatus.Cancelled;
                        world.AddLog($"[Скорость] {actor.Name} (ETA: {ev.Duration:0.0}s) перехватил клетку {ev.TargetCoords.ToString()} у медленного {slowerActor.Name} (оставалось ETA: {reservation.Eta:0.0}s)!");
                    }
                }

                // Обновляем структуру в словаре (поскольку это структура, перезаписываем её целиком)
                _tileReservations[ev.TargetCoords] = new MoveReservation
                {
                    ActiveActor = actor.Id,
                    Eta = ev.Duration
                };
            }
            else
            {
                world.AddLog($"[Скорость] {actor.Name} (ETA: {ev.Duration:0.0}s) отменил движение на {ev.TargetCoords.ToString()}: соперник прибудет туда раньше (осталось ETA: {reservation.Eta:0.0}s).");
                ev.Status = EventStatus.Cancelled;
                return;
            }
        }
        else
        {
            // Создаем новую структуру-бронь на стеке и кладем в словарь
            _tileReservations[ev.TargetCoords] = new MoveReservation
            {
                ActiveActor = actor.Id,
                Eta = ev.Duration
            };
            world.AddLog($"[Бронь] {actor.Name} зарезервировал клетку {ev.TargetCoords.ToString()} (Время до прибытия: {ev.Duration:0.0}s).");
        }
    }

    protected override void OnUpdate(float deltaTime, Actor actor, MoveEvent ev, WorldState world)
    {
        ev.ElapsedTime += deltaTime;

        if (ev.ElapsedTime >= ev.Duration)
        {
            actor.Position = ev.TargetCoords;
            ev.Status = EventStatus.Completed;
        }
    }

    public override void Update(float deltaTime, WorldState world)
    {
        // Поиск по ключам и обновление структур
        foreach (var (coords, reservation) in _tileReservations)
        {
            var updated = reservation;
            updated.Eta = Math.Max(0f, reservation.Eta - deltaTime);
            _tileReservations[coords] = updated;
        }

        base.Update(deltaTime, world);
    }

    public void RemoveReservation(EntityId reserver, HexCoords coords)
    {
        if (_tileReservations.TryGetValue(coords, out var reservation) && reservation.ActiveActor == reserver)
        {
            _tileReservations.Remove(coords);
        }
    }
}