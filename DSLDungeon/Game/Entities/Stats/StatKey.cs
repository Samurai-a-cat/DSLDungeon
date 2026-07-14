namespace DSLDungeon.Game.Entities.Stats;

/// <summary>
/// Ключ стата. В памяти — int (быстро), на диске — читаемая строка.
/// ID генерируются автоматически при первом обращении, руками не трогать!
/// </summary>
public readonly record struct StatKey : IEquatable<StatKey>
{
    private readonly int _id;

    public int Id => _id;

    // Internal — чтобы StatKeyRegistry мог создавать при парсинге
    internal StatKey(int id) => _id = id;

    // --- Предопределённые ключи (ID ставятся автоматом) ---
    public static readonly StatKey Strength = Register("Strength");
    public static readonly StatKey Dexterity = Register("Dexterity");
    public static readonly StatKey Intelligence = Register("Intelligence");
    public static readonly StatKey Constitution = Register("Constitution");

    public static readonly StatKey CritChance = Register("CritChance");
    public static readonly StatKey CritMultiplier = Register("CritMultiplier");
    public static readonly StatKey AttackSpeed = Register("AttackSpeed");
    public static readonly StatKey CastSpeed = Register("CastSpeed");
    public static readonly StatKey MoveSpeed = Register("MoveSpeed");

    public static readonly StatKey Armor = Register("Armor");
    public static readonly StatKey Evasion = Register("Evasion");
    public static readonly StatKey BlockChance = Register("BlockChance");
    public static readonly StatKey ResistancePhysical = Register("ResistancePhysical");
    public static readonly StatKey ResistanceFire = Register("ResistanceFire");
    public static readonly StatKey ResistanceCold = Register("ResistanceCold");
    public static readonly StatKey ResistanceLightning = Register("ResistanceLightning");

    public static readonly StatKey DamageBase = Register("DamageBase");
    public static readonly StatKey DamageAdded = Register("DamageAdded");
    public static readonly StatKey DamageMore = Register("DamageMore");
    public static readonly StatKey DamageFinal = Register("DamageFinal");

    public static readonly StatKey DamagePhysicalPct = Register("DamagePhysicalPct");
    public static readonly StatKey DamageFirePct = Register("DamageFirePct");
    public static readonly StatKey DamageColdPct = Register("DamageColdPct");
    public static readonly StatKey DamageLightningPct = Register("DamageLightningPct");

    public static readonly StatKey BackstabBonus = Register("BackstabBonus");
    public static readonly StatKey HeightAdvantage = Register("HeightAdvantage");

    // --- Генератор ID ---
    private static int _nextId = 1;

    private static StatKey Register(string name)
    {
        var key = new StatKey(_nextId++);
        StatKeyRegistry.Register(key._id, name);
        return key;
    }

    // --- Runtime регистрация (моды, контент) ---
    public static StatKey Create(string name)
    {
        if (StatKeyRegistry.TryParse(name, out var existing))
            return existing;
        return Register(name);
    }

    // --- Equality: сравнение по int ---
    public bool Equals(StatKey other) => _id == other._id;
    public override int GetHashCode() => _id;

    public static implicit operator int(StatKey key) => key._id;
    public static implicit operator StatKey(int id) => new(id);

    public override string ToString() => StatKeyRegistry.GetName(_id);
}
