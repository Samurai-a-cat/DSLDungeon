using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Core.Processes;

/// <summary>
/// Фоновый процесс: тикает таймеры импульса и комбо для всех живых акторов.
/// </summary>
[SystemOrder(25)]
public class CombatStateProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            if (actor.GetComponent<HealthComponent>() is { IsDead: true }) continue;

            var combat = actor.GetComponent<CombatStateComponent>();
            combat.TickImpulse(deltaTime);
            combat.TickCombo(deltaTime);
        }
    }
}
