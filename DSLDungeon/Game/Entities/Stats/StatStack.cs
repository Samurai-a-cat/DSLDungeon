namespace DSLDungeon.Game.Entities.Stats;

public class StatStack
{
    private readonly List<StatModifier> _modifiers = new();

    public IReadOnlyList<StatModifier> Modifiers => _modifiers;

    public void AddModifier(StatModifier mod) => _modifiers.Add(mod);

    public void RemoveModifiersFromSource(ModifierSource source)
    {
        _modifiers.RemoveAll(m => m.Source == source);
    }

    public void RemoveModifiersByTag(string tag)
    {
        _modifiers.RemoveAll(m => m.Tag == tag);
    }

    public void Clear() => _modifiers.Clear();

    public float Calculate()
    {
        float baseSum = 0, baseMult = 0, added = 0, addedMult = 0;
        float more = 1f, less = 1f, finalAdded = 0, finalMult = 0;

        foreach (var m in _modifiers)
        {
            switch (m.Type)
            {
                case ModifierType.Base: baseSum += m.Value; break;
                case ModifierType.BaseMultiplier: baseMult += m.Value; break;
                case ModifierType.Added: added += m.Value; break;
                case ModifierType.AddedMultiplier: addedMult += m.Value; break;
                case ModifierType.More: more *= m.Value; break;
                case ModifierType.Less: less *= m.Value; break;
                case ModifierType.FinalAdded: finalAdded += m.Value; break;
                case ModifierType.FinalMultiplier: finalMult += m.Value; break;
            }
        }

        float result = ((baseSum * (1 + baseMult)) + (added * (1 + addedMult))) * more * less;
        result += finalAdded;
        result *= (1 + finalMult);
        return result;
    }

    public float CalculateForTag(string tag)
    {
        var filtered = _modifiers.Where(m => m.Tag == null || m.Tag == tag).ToList();
        return CalculateWithModifiers(filtered);
    }

    private static float CalculateWithModifiers(List<StatModifier> mods)
    {
        float baseSum = 0, baseMult = 0, added = 0, addedMult = 0;
        float more = 1f, less = 1f, finalAdded = 0, finalMult = 0;

        foreach (var m in mods)
        {
            switch (m.Type)
            {
                case ModifierType.Base: baseSum += m.Value; break;
                case ModifierType.BaseMultiplier: baseMult += m.Value; break;
                case ModifierType.Added: added += m.Value; break;
                case ModifierType.AddedMultiplier: addedMult += m.Value; break;
                case ModifierType.More: more *= m.Value; break;
                case ModifierType.Less: less *= m.Value; break;
                case ModifierType.FinalAdded: finalAdded += m.Value; break;
                case ModifierType.FinalMultiplier: finalMult += m.Value; break;
            }
        }

        float result = ((baseSum * (1 + baseMult)) + (added * (1 + addedMult))) * more * less;
        result += finalAdded;
        result *= (1 + finalMult);
        return result;
    }
}
