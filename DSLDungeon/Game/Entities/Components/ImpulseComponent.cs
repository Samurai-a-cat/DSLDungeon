using DSLDungeon.Game.Entities.Combat;

namespace DSLDungeon.Game.Entities.Components;

public class ImpulseComponent : EntityComponent
{
    public float BonusDamagePercent { get; set; } = 0.25f;
    public float DurationSeconds { get; set; } = 2.0f;

    private float _remainingTime;
    private bool _isActive;

    public bool IsActive => _isActive && _remainingTime > 0;

    public void Activate()
    {
        _isActive = true;
        _remainingTime = DurationSeconds;

        // Пишем в CombatState, а не в StatSheet
        if (Owner.GetComponent<CombatStateComponent>() is { } combat)
        {
            combat.IsImpulseActive = true;
            combat.ImpulseBonus = BonusDamagePercent;
        }
    }

    public void Consume()
    {
        if (!_isActive) return;
        _isActive = false;

        if (Owner.GetComponent<CombatStateComponent>() is { } combat)
        {
            combat.IsImpulseActive = false;
            combat.ImpulseBonus = 0;
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!_isActive) return;

        _remainingTime -= deltaTime;
        if (_remainingTime <= 0)
        {
            Consume();
        }
    }
}