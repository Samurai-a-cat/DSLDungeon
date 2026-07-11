using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Components;

/// <summary>
/// Ярость берсерка: урон растёт по мере потери HP.
/// Классическая механика для хардкорных RPG.
/// </summary>
public class BerserkRageComponent : EntityComponent
{
    public float HpThresholdPercent { get; set; } = 0.5f;
    public float DamageBonusPerMissingHp { get; set; } = 0.02f;
    
    private float _lastHpPercent = 1.0f;

    public override void OnUpdate(float deltaTime)
    {
        if (Owner.GetComponent<HealthComponent>() is not { } health) return;
        if (Owner.GetComponent<StatsComponent>() is not { } stats) return;

        float currentHpPercent = (float)health.CurrentHp / health.MaxHp;
        
        // Только если HP ниже порога
        if (currentHpPercent < HpThresholdPercent)
        {
            float missingHpBelowThreshold = HpThresholdPercent - currentHpPercent;
            float bonus = missingHpBelowThreshold * DamageBonusPerMissingHp;
            
            // Убираем старый модификатор, добавляем новый
            stats.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
            stats.Stats.AddModifier(StatKeys.DamageMore, 
                StatModifier.More(1.0f + bonus, ModifierSource.PassiveSkill));
            
            if (Math.Abs(currentHpPercent - _lastHpPercent) > 0.05f)
            {
                // Логируем только при значительном изменении
                // (логирование будет в системе)
            }
        }
        else
        {
            stats.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
        }
        
        _lastHpPercent = currentHpPercent;
    }
}