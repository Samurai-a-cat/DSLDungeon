using System.Linq;

namespace DSLDungeon.Game.Entities.Stats;

public class StatSheet
{
    private readonly StatStack?[] _stats = new StatStack?[StatKeyRegistry.Capacity];
    private readonly Dictionary<int, StatStack> _overflow = new();

    public event Action<StatKey, float>? OnStatChanged;

    public StatStack GetOrCreate(StatKey key)
    {
        int id = key.Id;

        if (id < _stats.Length)
        {
            if (_stats[id] == null)
                _stats[id] = new StatStack();
            return _stats[id]!;
        }

        if (!_overflow.TryGetValue(id, out var stack))
        {
            stack = new StatStack();
            _overflow[id] = stack;
        }
        return stack;
    }

    public bool HasStat(StatKey key)
    {
        int id = key.Id;
        if (id < _stats.Length) return _stats[id] != null;
        return _overflow.ContainsKey(id);
    }

    public float GetValue(StatKey key)
    {
        int id = key.Id;

        if (id < _stats.Length && _stats[id] != null)
            return _stats[id]!.Calculate();

        if (_overflow.TryGetValue(id, out var stack))
            return stack.Calculate();

        return 0f;
    }

    public void AddModifier(StatKey key, StatModifier mod)
    {
        var stack = GetOrCreate(key);
        stack.AddModifier(mod);
        OnStatChanged?.Invoke(key, stack.Calculate());
    }

    public void RemoveModifiersFromSource(ModifierSource source)
    {
        for (int i = 0; i < _stats.Length; i++)
        {
            if (_stats[i] == null) continue;
            int beforeCount = _stats[i]!.Modifiers.Count;
            _stats[i]!.RemoveModifiersFromSource(source);
            if (_stats[i]!.Modifiers.Count != beforeCount)
                OnStatChanged?.Invoke(new StatKey(i), _stats[i]!.Calculate());
        }

        foreach (var kvp in _overflow.ToList())
        {
            int beforeCount = kvp.Value.Modifiers.Count;
            kvp.Value.RemoveModifiersFromSource(source);
            if (kvp.Value.Modifiers.Count != beforeCount)
                OnStatChanged?.Invoke(new StatKey(kvp.Key), kvp.Value.Calculate());
        }
    }

    public void RemoveModifiersByTag(string tag)
    {
        for (int i = 0; i < _stats.Length; i++)
        {
            if (_stats[i] == null) continue;
            int beforeCount = _stats[i]!.Modifiers.Count;
            _stats[i]!.RemoveModifiersByTag(tag);
            if (_stats[i]!.Modifiers.Count != beforeCount)
                OnStatChanged?.Invoke(new StatKey(i), _stats[i]!.Calculate());
        }

        foreach (var kvp in _overflow.ToList())
        {
            int beforeCount = kvp.Value.Modifiers.Count;
            kvp.Value.RemoveModifiersByTag(tag);
            if (kvp.Value.Modifiers.Count != beforeCount)
                OnStatChanged?.Invoke(new StatKey(kvp.Key), kvp.Value.Calculate());
        }
    }

    public void InitializeBaseStats(Dictionary<StatKey, float> baseValues)
    {
        foreach (var kvp in baseValues)
            GetOrCreate(kvp.Key).AddModifier(StatModifier.Base(kvp.Value));
    }

    public Dictionary<StatKey, float> GetAllCalculatedValues()
    {
        var result = new Dictionary<StatKey, float>();
        for (int i = 0; i < _stats.Length; i++)
        {
            if (_stats[i] != null)
                result[new StatKey(i)] = _stats[i]!.Calculate();
        }
        foreach (var kvp in _overflow)
            result[new StatKey(kvp.Key)] = kvp.Value.Calculate();
        return result;
    }

    // --- Сериализация: читаемые строки на диске ---
    public Dictionary<string, float> Serialize()
    {
        var result = new Dictionary<string, float>();
        for (int i = 0; i < _stats.Length; i++)
        {
            if (_stats[i] != null)
                result[StatKeyRegistry.GetName(i)] = _stats[i]!.Calculate();
        }
        foreach (var kvp in _overflow)
            result[StatKeyRegistry.GetName(kvp.Key)] = kvp.Value.Calculate();
        return result;
    }

    public void Deserialize(Dictionary<string, float> data)
    {
        foreach (var kvp in data)
        {
            if (StatKeyRegistry.TryParse(kvp.Key, out var key))
                GetOrCreate(key).AddModifier(StatModifier.Base(kvp.Value));
        }
    }
}
