using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public class Actor : Entity
{
    public HealthComponent Health => GetComponent<HealthComponent>();
    public StatsComponent Stats => GetComponent<StatsComponent>();
    public CombatStateComponent Combat => GetComponent<CombatStateComponent>();
    public AbilityCooldownComponent Cooldowns => GetComponent<AbilityCooldownComponent>();
    public PositionTrackerComponent PositionTracker => GetComponent<PositionTrackerComponent>();

    public EventQueue Queue { get; } = new();

    public Actor(EntityId id, string name, HexCoords position) : base(id, name, position)
    {
        AddComponent(new HealthComponent());
        AddComponent(new StatsComponent());
        AddComponent(new CombatStateComponent());
        AddComponent(new AbilityCooldownComponent());
        AddComponent(new PositionTrackerComponent());
    }
}
