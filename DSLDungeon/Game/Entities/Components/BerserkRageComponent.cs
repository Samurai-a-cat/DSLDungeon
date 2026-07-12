using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Components;

public class BerserkRageComponent : EntityComponent
{
    public float HpThresholdPercent { get; set; } = 0.5f;
    public float DamageBonusPerMissingHp { get; set; } = 0.02f;
    
    private float _lastHpPercent = 1.0f;
    private bool _wasSubThreshold = false;

    public override void OnAttached(Entity owner)
    {
        base.OnAttached(owner);
        
        // Подписываемся на изменение HP вместо опроса каждый кадр
        if (owner.GetComponent<HealthComponent>() is { } health)
        {
            health.OnHpChanged += OnHpChanged;
        }
    }

    public override void OnDetached()
    {
        if (Owner.GetComponent<HealthComponent>() is { } health)
        {
            health.OnHpChanged -= OnHpChanged;
        }
        base.OnDetached();
    }

    private void OnHpChanged(int currentHp, int maxHp)
    {
        if (Owner.GetComponent<StatsComponent>() is not { } stats) return;
        
        float currentHpPercent = (float)currentHp / maxHp;
        bool isSubThreshold = currentHpPercent < HpThresholdPercent;

        if (isSubThreshold)
        {
            float missingHpBelowThreshold = HpThresholdPercent - currentHpPercent;
            float bonus = missingHpBelowThreshold * DamageBonusPerMissingHp;
            
            // Убираем старый, добавляем новый — только если изменился существенно
            stats.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
            stats.Stats.AddModifier(StatKeys.DamageMore, 
                StatModifier.More(1.0f + bonus, ModifierSource.PassiveSkill));
        }
        else if (_wasSubThreshold)
        {
            // Вышли из порога — чистим
            stats.Stats.RemoveModifiersFromSource(ModifierSource.PassiveSkill);
        }
        
        _wasSubThreshold = isSubThreshold;
        _lastHpPercent = currentHpPercent;
    }

    public override void OnUpdate(float deltaTime) { }
}