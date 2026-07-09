using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Characters;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Core.Actions.Systems;

// --- УНИФИЦИРОВАННОЕ СОБЫТИЕ СПАВНА ---
[PoolConfig(10)]
public class SpawnUnitEvent : QueueEvent<UnitSpawnerSystem>
{
    public override int Priority => 4; // Средний приоритет

    public string UnitType { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public HexCoords SpawnCoords { get; set; }

    public override void Reset()
    {
        base.Reset();
        UnitType = string.Empty;
        UnitName = string.Empty;
        SpawnCoords = default;
    }
}

// --- УНИФИЦИРОВАННАЯ СИСТЕМА СПАВНА ---
[SystemOrder(40)] // Выполняется в фазе обработки действий
public class UnitSpawnerSystem : GameSystem<SpawnUnitEvent>, IGameSystem
{
    // Расширяем стандартный Update, чтобы он проверял И очередь мира, И очереди персонажей
    public override void Update(float deltaTime, WorldState world)
    {
        // 1. Сначала проверяем глобальную очередь мира (спавн от Рассказчика)
        if (world.WorldQueue.GetActiveEvent() is SpawnUnitEvent globalSpawn)
        {
            if (globalSpawn.Status == EventStatus.Pending)
            {
                globalSpawn.Status = EventStatus.Executing;
                ExecuteSpawn(globalSpawn, world);
                globalSpawn.Status = EventStatus.Completed;
            }
        }

        // 2. Затем запускаем базовый Update для обработки призывов локальных акторов (игроков)
        base.Update(deltaTime, world);
    }

    // Вызывается для локального актора (например, Рыцарь кастует призыв)
    protected override void OnUpdate(float deltaTime, Actor actor, SpawnUnitEvent ev, WorldState world)
    {
        ExecuteSpawn(ev, world);
        ev.Status = EventStatus.Completed;
    }

    // Единое место материализации существа в мире
    private void ExecuteSpawn(SpawnUnitEvent ev, WorldState world)
    {
        var id = EntityIdGenerator.Next();
        Actor newUnit;

        // В будущем здесь будет обращение к фабрике шаблонов существ (архетипов)
        if (ev.UnitType == "Orc")
        {
            newUnit = new Orc(id, ev.UnitName, ev.SpawnCoords, world, maxHp: 40);
        }
        else if (ev.UnitType == "SummonedMinion")
        {
            newUnit = new Hero(id, ev.UnitName, ev.SpawnCoords, world, maxHp: 30); // Временный союзный юнит
        }
        else
        {
            Console.WriteLine($"[Спавнер] Ошибка: Неизвестный тип юнита {ev.UnitType}");
            return;
        }

        world.SpawnEntity(newUnit);
        Console.WriteLine($"[Спавнер] Юнит '{newUnit.Name}' ({newUnit.Id}) материализовался в координате {newUnit.Position}");
    }
}