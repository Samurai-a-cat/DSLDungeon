using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Items;

public abstract class Item
{
    public string Name { get; }
    public string? Description { get; set; }

    private readonly List<(string StatKey, StatModifier Modifier)> _modifiers = new();
    public IReadOnlyList<(string StatKey, StatModifier Modifier)> Modifiers => _modifiers;

    protected Item(string name)
    {
        Name = name;
    }

    public void AddModifier(string statKey, StatModifier mod) => _modifiers.Add((statKey, mod));

    public Dictionary<string, StatModifier> GetModifiers()
    {
        var result = new Dictionary<string, StatModifier>();
        foreach (var (key, mod) in _modifiers)
        {
            result[key] = mod;
        }
        return result;
    }
}
