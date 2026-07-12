using System;

namespace DSLDungeon.Game.Entities.Components;

public class BackgroundThreadComponent : EntityComponent
{
    public float CheckInterval { get; set; } = 3.0f;
    public required Func<Actor, WorldState, bool> Condition { get; set; }
    public required Action<Actor, WorldState> OnTrigger { get; set; }

    private float _timer;
    private WorldState? _world;

    public void Initialize(WorldState world) => _world = world;

    public override void OnUpdate(float deltaTime)
    {
        if (_world == null) return;
        if (Owner is not Actor actor) return;
        if (actor.GetComponent<HealthComponent>() is { IsDead: true }) return;

        _timer += deltaTime;
        if (_timer >= CheckInterval)
        {
            _timer = 0;
            if (Condition(actor, _world))
            {
                OnTrigger(actor, _world);
            }
        }
    }
}