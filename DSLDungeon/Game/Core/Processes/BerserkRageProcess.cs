using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Core.Processes;

/// <summary>
/// Фоновый процесс: проверяет HP акторов с BerserkRageData и применяет/снимает модификатор урона.
/// </summary>
[SystemOrder(20)]
public class BerserkRageProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            var data = actor.GetComponent<BerserkRageData>();
            var health = actor.GetComponent<HealthComponent>();
            var stats = actor.GetComponent<StatsComponent>();

            float hpPercent = (float)health.CurrentHp / health.MaxHp;

            if (hpPercent < data.HpThresholdPercent)
            {
                float missing = data.HpThresholdPercent - hpPercent;
                float bonus = missing * data.DamageBonusPerMissingHp;
                stats.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
                stats.Stats.AddModifier(StatKeys.DamageMore,
                    StatModifier.More(1.0f + bonus, ModifierSource.PassiveSkill));
            }
            else
            {
                stats.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
            }
        }
    }
}
