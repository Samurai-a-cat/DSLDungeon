using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Time;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game;

public class GameLoop
{
    private readonly WorldState _world;
    private readonly TimeSystem _timeSystem = new();
    private readonly TimeChannel _gameTimeChannel;

    public TimeChannel GameTimeChannel => _gameTimeChannel;
    public TimeSystem Time => _timeSystem;

    public GameLoop(WorldState world)
    {
        _world = world;
        _gameTimeChannel = _timeSystem.CreateChannel();
        EventPool.Initialize();
    }

    public void Update()
    {
        _timeSystem.Update();
        float dt = _gameTimeChannel.Delta.Value;

        if (dt <= 0) return;

        // Обновляем компоненты всех акторов (ИИ)
        foreach (var actor in _world.GetAllActors())
        {
            if (actor.Health.IsDead) continue;
            actor.UpdateComponents(dt);
        }

        _world.Systems.Update(dt, _world);

        foreach (var actor in _world.GetAllActors())
        {
            actor.Queue.CleanUp(_world);
        }

        _world.WorldQueue.CleanUp(_world);
        _world.FinalizeDespawns();
    }
}
