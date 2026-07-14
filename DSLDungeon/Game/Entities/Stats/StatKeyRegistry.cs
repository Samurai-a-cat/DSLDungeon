namespace DSLDungeon.Game.Entities.Stats;

/// <summary>
/// Реестр ключей статов. Хранит маппинг int → строка для сериализации.
/// </summary>
public static class StatKeyRegistry
{
    private static string[] _names = new string[64];
    private static readonly Dictionary<string, int> _nameToId = new();

    public static int Capacity => _names.Length;

    public static void Register(int id, string name)
    {
        if (id >= _names.Length)
            Array.Resize(ref _names, Math.Max(id * 2, 64));
        _names[id] = name;
        _nameToId[name] = id;
    }

    public static string GetName(int id) => 
        id < _names.Length && _names[id] != null 
            ? _names[id]! 
            : $"unknown_{id}";

    public static bool TryParse(string name, out StatKey key)
    {
        if (_nameToId.TryGetValue(name, out var id))
        {
            key = new StatKey(id);
            return true;
        }
        key = default;
        return false;
    }

    public static StatKey Parse(string name) => 
        TryParse(name, out var key) ? key : throw new ArgumentException($"Unknown stat key: {name}");
}
