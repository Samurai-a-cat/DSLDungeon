using DSLDungeon.Game.Entities.Combat;

namespace DSLDungeon.Game.Entities.Components;

public class ComboComponent : EntityComponent
{
    public int Counter { get; private set; }
    public Entity? CurrentTarget { get; private set; }

    public float ResetTime { get; set; } = 3.0f;
    private float _timer;

    public void OnHit(Entity target)
    {
        if (CurrentTarget?.Id != target.Id)
        {
            Counter = 1;
            CurrentTarget = target;
        }
        else
        {
            Counter++;
        }

        _timer = 0;

        // Пишем в CombatState, а не в StatSheet
        if (Owner.GetComponent<CombatStateComponent>() is { } combat)
        {
            combat.ComboCount = Counter;
            combat.ComboTarget = target;
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Counter <= 0) return;

        _timer += deltaTime;
        if (_timer >= ResetTime)
        {
            Counter = 0;
            CurrentTarget = null;

            if (Owner.GetComponent<CombatStateComponent>() is { } combat)
            {
                combat.ComboCount = 0;
                combat.ComboTarget = null;
            }
        }
    }
}