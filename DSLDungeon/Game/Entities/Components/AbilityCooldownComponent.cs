namespace DSLDungeon.Game.Entities.Components;

/// <summary>
/// Тупо данные: откаты способностей. AbilityId -> оставшиеся секунды.
/// Логика таймеров в AbilityCooldownProcess.
/// </summary>
public class AbilityCooldownComponent : EntityComponent
{
    public readonly Dictionary<string, float> Cooldowns = new();
}
