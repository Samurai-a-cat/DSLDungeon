using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Items;

public abstract class Item
{
    public string Name { get; }
    public string? Description { get; set; }

    private readonly List<(StatKey Key, StatModifier Modifier)> _modifiers = new();
    public IReadOnlyList<(StatKey Key, StatModifier Modifier)> Modifiers => _modifiers;

    protected Item(string name)
    {
        Name = name;
    }

    public void AddModifier(StatKey key, StatModifier mod) => _modifiers.Add((key, mod));

    public Dictionary<StatKey, StatModifier> GetModifiers()
    {
        var result = new Dictionary<StatKey, StatModifier>();
        foreach (var (key, mod) in _modifiers)
        {
            result[key] = mod;
        }
        return result;
    }
}
