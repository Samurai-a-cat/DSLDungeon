using System;
using System.Collections.Generic;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Core.Actions;

// --- СОБЫТИЕ ПЕРЕМЕЩЕНИЯ ---
[PoolConfig(10)]
public class MoveEvent : QueueEvent<MovementSystem>
{
    public override int Priority => 5; // Обычный приоритет движения

    public HexCoords TargetCoords { get; set; }
    public float Duration { get; set; }
    public float ElapsedTime { get; set; }

    // При выходе события из очереди (завершение/отмена) обязательно освобождаем бронь
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

// --- СИСТЕМА ДВИЖЕНИЯ И ДИНАМИЧЕСКИХ КОЛЛИЗИЙ ---
public class MovementSystem : GameSystem<MoveEvent>, IEntityTrackingSystem, IGameSystem
{
    // Вложенный класс для хранения параметров брони
    private class MoveReservation
    {
        public EntityId ActiveActor { get; set; }
        public float Eta { get; set; }
    }

    // Быстрый реестр зарезервированных гексов
    private readonly Dictionary<HexCoords, MoveReservation> _tileReservations = new();

    // Регистрация сущностей в системе
    public new void Register(EntityId id) => base.Register(id);
    public new void Unregister(EntityId id) => base.Unregister(id);

    /// <summary>
    /// Вызывается один раз при старте движения. Здесь происходит валидация и конкуренция за клетку.
    /// </summary>
    protected override void OnStart(Actor actor, MoveEvent ev, WorldState world)
    {
        // 1. Проверяем, свободен ли целевой тайл на карте
        if (world.Map.TryGetTile(ev.TargetCoords, out var tile) && !tile.IsPassable)
        {
            world.AddLog($"[Препятствие] {actor.Name} не может наступить на непроходимый тайл {ev.TargetCoords.ToString()}.");
            ev.Status = EventStatus.Cancelled;
            return;
        }

        // 2. Проверяем, стоит ли кто-то физически на этой клетке прямо сейчас
        var physicalOccupant = world.GetEntityAt(ev.TargetCoords);
        if (physicalOccupant != null && physicalOccupant.Id != actor.Id)
        {
            // Если там кто-то стоит, проверяем, уходит ли он прямо сейчас (есть ли у него движение)
            if (physicalOccupant is Actor occupantActor && occupantActor.Queue.GetActiveEvent() is MoveEvent occupantMove)
            {
                // Он уходит. Считаем, успеем ли мы врезаться в него до его ухода
                float occupantTimeLeft = occupantMove.Duration - occupantMove.ElapsedTime;
                if (ev.Duration < occupantTimeLeft)
                {
                    // Мы слишком быстрые, врежемся в него сзади. Отменяем наше движение.
                    world.AddLog($"[Конфликт] {actor.Name} прервал движение: клетка {ev.TargetCoords.ToString()} еще занята уходящим {occupantActor.Name}.");
                    ev.Status = EventStatus.Cancelled;
                    return;
                }
            }
            else
            {
                // На клетке кто-то стоит стационарно и никуда не идет
                world.AddLog($"[Конфликт] {actor.Name} не может пойти на {ev.TargetCoords.ToString()}: клетка занята {physicalOccupant.Name}.");
                ev.Status = EventStatus.Cancelled;
                return;
            }
        }

        // 3. Конфликт бронирования (Если два юнита одновременно идут на одну и ту же клетку)
        if (_tileReservations.TryGetValue(ev.TargetCoords, out var reservation))
        {
            // Клетка уже зарезервирована другим идущим юнитом. Сравниваем время прибытия (ETA).
            if (ev.Duration < reservation.Eta)
            {
                // МЫ БЫСТРЕЕ! Мы отбираем бронь у медленного юнита.
                if (world.TryGetEntity(reservation.ActiveActor, out var slowerEntity) && slowerEntity is Actor slowerActor)
                {
                    if (slowerActor.Queue.GetActiveEvent() is MoveEvent slowerMove)
                    {
                        // Принудительно прерываем движение медленного соперника
                        slowerMove.Status = EventStatus.Cancelled;
                        world.AddLog($"[Скорость] {actor.Name} (ETA: {ev.Duration:0.0}s) перехватил клетку {ev.TargetCoords.ToString()} у медленного {slowerActor.Name} (оставалось ETA: {reservation.Eta:0.0}s)!");
                    }
                }

                // Перезаписываем бронь на себя
                reservation.ActiveActor = actor.Id;
                reservation.Eta = ev.Duration;
            }
            else
            {
                // МЫ МЕДЛЕННЕЕ. Клетка достанется сопернику, прибывающему раньше. Наше движение отменяется.
                world.AddLog($"[Скорость] {actor.Name} (ETA: {ev.Duration:0.0}s) отменил движение на {ev.TargetCoords.ToString()}: соперник прибудет туда раньше (осталось ETA: {reservation.Eta:0.0}s).");
                ev.Status = EventStatus.Cancelled;
                return;
            }
        }
        else
        {
            // Клетка полностью свободна. Создаем новую бронь.
            _tileReservations[ev.TargetCoords] = new MoveReservation
            {
                ActiveActor = actor.Id,
                Eta = ev.Duration
            };
            world.AddLog($"[Бронь] {actor.Name} зарезервировал клетку {ev.TargetCoords.ToString()} (Время до прибытия: {ev.Duration:0.0}s).");
        }
    }

    /// <summary>
    /// Вызывается каждый кадр для симуляции перемещения
    /// </summary>
    protected override void OnUpdate(float deltaTime, Actor actor, MoveEvent ev, WorldState world)
    {
        ev.ElapsedTime += deltaTime;

        if (ev.ElapsedTime >= ev.Duration)
        {
            // Физически перемещаем персонажа на целевую клетку в конце пути
            actor.Position = ev.TargetCoords;
            ev.Status = EventStatus.Completed;
        }
    }

    /// <summary>
    /// Системное обновление. Уменьшает ETA для всех зарезервированных гексов.
    /// </summary>
    public override void Update(float deltaTime, WorldState world)
    {
        // Сначала обновляем таймеры ETA для всех броней в мире
        foreach (var reservation in _tileReservations.Values)
        {
            reservation.Eta = Math.Max(0f, reservation.Eta - deltaTime);
        }

        // Запускаем стандартный цикл симуляции перемещений
        base.Update(deltaTime, world);
    }

    /// <summary>
    /// Безопасно снимает бронь с клетки. Вызывается автоматически при OnCleanUp события.
    /// </summary>
    public void RemoveReservation(EntityId reserver, HexCoords coords)
    {
        // Удаляем бронь только в том случае, если она до сих пор принадлежит этому ререзверу
        if (_tileReservations.TryGetValue(coords, out var reservation) && reservation.ActiveActor == reserver)
        {
            _tileReservations.Remove(coords);
        }
    }
}