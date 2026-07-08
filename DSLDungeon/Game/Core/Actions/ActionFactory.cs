namespace DSLDungeon.Game.Core.Actions;

public static class ActionFactory
{
    /// <summary>
    /// Создает команду из пула и вызывает для неё переданный инициализатор.
    /// </summary>
    public static T Create<T>(Action<T> initializer) where T : ActionCommand, new()
    {
        var cmd = ActionPool.Get<T>();
        initializer(cmd);
        return cmd;
    }
}