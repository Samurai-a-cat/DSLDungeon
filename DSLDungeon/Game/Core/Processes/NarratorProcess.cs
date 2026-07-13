using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Actions.Systems;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Core.Processes;

[SystemOrder(90)]
public class NarratorProcess : IGameSystem
{
    private int _currentWave = 1;
    private bool _waveInProgress = true;
    private readonly Random _random = new();

    public void Update(float deltaTime, WorldState world)
    {
        if (_waveInProgress)
        {
            bool anyOrcAlive = false;
            foreach (var actor in world.GetAllActors())
            {
                if (actor.Name.Contains("Орк") && !actor.Health.IsDead)
                {
                    anyOrcAlive = true;
                    break;
                }
            }

            if (!anyOrcAlive)
            {
                _waveInProgress = false;
                _currentWave++;

                world.AddLog($"[Режиссер] Волна зачищена! Мобилизация сил для Волны {_currentWave}...");

                HexCoords spawnCoords = GetRandomPassableCoords(world);

                var spawnEvent = EventPool.Get<SpawnUnitEvent>();
                spawnEvent.Owner = EntityId.None;
                spawnEvent.UnitType = "Orc";
                spawnEvent.UnitName = $"Орк Волны {_currentWave}";
                spawnEvent.SpawnCoords = spawnCoords;

                world.WorldQueue.Enqueue(spawnEvent, world);
                _waveInProgress = true;
            }
        }
    }

    private HexCoords GetRandomPassableCoords(WorldState world)
    {
        var candidates = new List<HexCoords>();

        foreach (var tile in world.Map.GetAllTiles())
        {
            if (tile.IsPassable && world.GetEntityAt(tile.Coords) == null)
            {
                candidates.Add(tile.Coords);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[_random.Next(candidates.Count)];
        }

        return new HexCoords(2, 0);
    }
}
