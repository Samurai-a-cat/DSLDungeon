using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Core.Processes;

/// <summary>
/// Фоновый процесс: уменьшает откаты способностей каждый кадр.
/// </summary>
[SystemOrder(10)]
public class AbilityCooldownProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            var cd = actor.GetComponent<AbilityCooldownComponent>();

            var keys = new List<string>(cd.Cooldowns.Keys);
            foreach (var key in keys)
            {
                cd.Cooldowns[key] -= deltaTime;
                if (cd.Cooldowns[key] <= 0)
                    cd.Cooldowns.Remove(key);
            }
        }
    }
}
