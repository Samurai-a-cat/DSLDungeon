namespace DSLDungeon.Game.DSL;

/// <summary>
/// Лёгкий DTO с информацией о сущности для скрипта.
/// </summary>
public readonly record struct DslEntityInfo(
    string Name,
    int Hp,
    int MaxHp,
    int Q,
    int R,
    bool IsEnemy
);
