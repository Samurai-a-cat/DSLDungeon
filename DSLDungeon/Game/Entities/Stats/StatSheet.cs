namespace DSLDungeon.Game.Entities.Stats;

public class StatSheet
{
    private readonly Dictionary<string, StatStack> _stats = new();

    public event Action<string, float>? OnStatChanged;

    public StatStack GetOrCreate(string key)
    {
        if (!_stats.TryGetValue(key, out var stack))
        {
            stack = new StatStack();
            _stats[key] = stack;
        }
        return stack;
    }

    public bool HasStat(string key) => _stats.ContainsKey(key);

    public float GetValue(string key)
    {
        if (_stats.TryGetValue(key, out var stack))
            return stack.Calculate();
        return 0f;
    }

    public void AddModifier(string key, StatModifier mod)
    {
        var stack = GetOrCreate(key);
        stack.AddModifier(mod);
        OnStatChanged?.Invoke(key, stack.Calculate());
    }

    public void RemoveModifiersFromSource(ModifierSource source)
    {
        foreach (var kvp in _stats)
        {
            int beforeCount = kvp.Value.Modifiers.Count;
            kvp.Value.RemoveModifiersFromSource(source);
            if (kvp.Value.Modifiers.Count != beforeCount)
            {
                OnStatChanged?.Invoke(kvp.Key, kvp.Value.Calculate());
            }
        }
    }

    public void RemoveModifiersByTag(string tag)
    {
        foreach (var kvp in _stats)
        {
            int beforeCount = kvp.Value.Modifiers.Count;
            kvp.Value.RemoveModifiersByTag(tag);
            if (kvp.Value.Modifiers.Count != beforeCount)
            {
                OnStatChanged?.Invoke(kvp.Key, kvp.Value.Calculate());
            }
        }
    }

    public void InitializeBaseStats(Dictionary<string, float> baseValues)
    {
        foreach (var kvp in baseValues)
        {
            GetOrCreate(kvp.Key).AddModifier(StatModifier.Base(kvp.Value));
        }
    }

    public Dictionary<string, float> GetAllCalculatedValues()
    {
        var result = new Dictionary<string, float>();
        foreach (var kvp in _stats)
        {
            result[kvp.Key] = kvp.Value.Calculate();
        }
        return result;
    }
}
