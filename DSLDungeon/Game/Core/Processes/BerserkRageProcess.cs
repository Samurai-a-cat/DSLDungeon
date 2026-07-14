using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Core.Processes;

[SystemOrder(20)]
public class BerserkRageProcess : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        foreach (var actor in world.GetAllActors())
        {
            if (!actor.TryGetComponent<BerserkRageData>(out var data)) continue;

            float hpPercent = (float)actor.Health.CurrentHp / actor.Health.MaxHp;

            if (hpPercent < data.HpThresholdPercent)
            {
                float missing = data.HpThresholdPercent - hpPercent;
                float bonus = missing * data.DamageBonusPerMissingHp;
                actor.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
                actor.Stats.AddModifier(StatKey.DamageMore,
                    StatModifier.More(1.0f + bonus, ModifierSource.PassiveSkill));
            }
            else
            {
                actor.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
            }
        }
    }
}
