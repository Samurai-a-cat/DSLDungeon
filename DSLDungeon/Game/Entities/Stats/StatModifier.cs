namespace DSLDungeon.Game.Entities.Stats;

public readonly record struct StatModifier
{
    public readonly float Value;
    public readonly ModifierType Type;
    public readonly ModifierSource Source;
    public readonly string? Tag;
    public readonly object? Context;

    public StatModifier(float value, ModifierType type, ModifierSource source, string? tag = null, object? context = null)
    {
        Value = value;
        Type = type;
        Source = source;
        Tag = tag;
        Context = context;
    }

    public static StatModifier Base(float value, string? tag = null) => 
        new(value, ModifierType.Base, ModifierSource.BaseStats, tag);

    public static StatModifier Added(float value, ModifierSource source, string? tag = null) => 
        new(value, ModifierType.Added, source, tag);

    public static StatModifier More(float value, ModifierSource source, string? tag = null) => 
        new(value, ModifierType.More, source, tag);

    public static StatModifier Less(float value, ModifierSource source, string? tag = null) => 
        new(value, ModifierType.Less, source, tag);

    public static StatModifier FinalMultiplier(float value, ModifierSource source, string? tag = null) => 
        new(value, ModifierType.FinalMultiplier, source, tag);
}
