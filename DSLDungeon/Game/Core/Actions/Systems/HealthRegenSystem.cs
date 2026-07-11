using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Particles;

namespace DSLDungeon.Game.Core.Actions.Systems;

[SystemOrder(60)]
public class HealthRegenSystem : IGameSystem
{
    private float _accumulatedTime;

    public void Update(float deltaTime, WorldState world)
    {
        _accumulatedTime += deltaTime;

        if (_accumulatedTime >= 1.0f)
        {
            _accumulatedTime -= 1.0f;

            foreach (var actor in world.GetAllActors())
            {
                var hp = actor.GetComponent<HealthComponent>();

                if (hp is { IsDead: false } && hp.CurrentHp < hp.MaxHp)
                {
                    int oldHp = hp.CurrentHp;
                    hp.ModifyHp(hp.RegenRate);

                    int diff = hp.CurrentHp - oldHp;
                    if (diff > 0)
                    {
                        world.AddLog($"[Регенерация] {actor.Name} восстановил {diff} HP.");

                        world.PendingDamageTriggers.Add(new VisualDamageTrigger
                        {
                            Coords = actor.Position,
                            Text = $"+{diff}",
                            Type = "Heal"
                        });
                    }
                }
            }
        }
    }
}
