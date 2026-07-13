using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Particles;

namespace DSLDungeon.Game.Core.Processes;

[SystemOrder(60)]
public class HealthRegenProcess : IGameSystem
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
                var hp = actor.Health;

                if (!hp.IsDead && hp.CurrentHp < hp.MaxHp)
                {
                    int oldHp = hp.CurrentHp;
                    hp.ModifyHp(hp.RegenRate);

                    int diff = hp.CurrentHp - oldHp;
                    if (diff > 0)
                    {
                        world.AddLog($"[Регенерация] {actor.Name} восстановил {diff} HP.");

                        world.PendingDamageTriggers.Add(new VisualDamageTrigger(
                            actor.Position,
                            $"+{diff}",
                            "Heal"
                        ));
                    }
                }
            }
        }
    }
}
