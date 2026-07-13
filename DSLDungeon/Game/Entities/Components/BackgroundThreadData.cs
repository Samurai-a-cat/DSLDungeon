namespace DSLDungeon.Game.Entities.Components;

/// <summary>
/// Данные фонового потока (магический интеллект).
/// Логика в BackgroundThreadProcess.
/// </summary>
public class BackgroundThreadData : EntityComponent
{
    public float CheckInterval { get; set; } = 3.0f;
    public required Func<Actor, WorldState, bool> Condition { get; set; }
    public required Action<Actor, WorldState> OnTrigger { get; set; }
    public float AccumulatedTime { get; set; }
}
