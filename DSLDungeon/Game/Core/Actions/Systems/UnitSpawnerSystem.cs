using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Characters;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Core.Actions.Systems;

[PoolConfig(10)]
public class SpawnUnitEvent : SystemEvent<UnitSpawnerSystem>
{
    public override int Priority => 4;

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

/// <summary>
/// Системная обработка спавна: не абилка персонажа.
/// </summary>
[SystemOrder(40)]
public class UnitSpawnerSystem : GameSystem<SpawnUnitEvent>, IGameSystem
{
    public override void Update(float deltaTime, WorldState world)
    {
        if (world.WorldQueue.GetActiveEvent() is SpawnUnitEvent globalSpawn)
        {
            if (globalSpawn.Status == EventStatus.Pending)
            {
                globalSpawn.Status = EventStatus.Executing;
                ExecuteSpawn(globalSpawn, world);
                globalSpawn.Status = EventStatus.Completed;
            }
        }

        base.Update(deltaTime, world);
    }

    protected override void OnUpdate(float deltaTime, Actor actor, SpawnUnitEvent ev, WorldState world)
    {
        ExecuteSpawn(ev, world);
        ev.Status = EventStatus.Completed;
    }

    private void ExecuteSpawn(SpawnUnitEvent ev, WorldState world)
    {
        var id = EntityIdGenerator.Next();
        Actor newUnit;

        if (ev.UnitType == "Orc")
        {
            var orc = OrcFactory.CreateGrunt(id, ev.UnitName, ev.SpawnCoords, world, waveLevel: 1);

            var dagger = WeaponPresets.CreateRustyDagger();
            orc.GetComponent<EquipmentComponent>()?.Equip(EquipmentSlot.MainHand, dagger);

            newUnit = orc;
        }
        else if (ev.UnitType == "SummonedMinion")
        {
            newUnit = HeroFactory.CreateKnight(id, ev.SpawnCoords, world);
        }
        else
        {
            System.Console.WriteLine($"[Спавнер] Ошибка: Неизвестный тип юнита {ev.UnitType}");
            return;
        }

        world.SpawnEntity(newUnit);
        world.AddLog($"[Спавнер] Юнит '{newUnit.Name}' ({newUnit.Id}) материализовался в координате {newUnit.Position}");
    }
}
