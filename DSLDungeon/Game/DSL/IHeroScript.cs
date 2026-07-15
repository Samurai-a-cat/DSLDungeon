namespace DSLDungeon.Game.DSL;

/// <summary>
/// Интерфейс, который должен реализовать скрипт игрока.
/// </summary>
public interface IHeroScript
{
    void Tick(DslContext context);
}
