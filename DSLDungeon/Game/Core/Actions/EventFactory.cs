namespace DSLDungeon.Game.Core.Actions;

public static class EventFactory
{
    public static T Create<T>(EntityId owner, Action<T> initializer) where T : class, IQueueEvent, new()
    {
        var ev = EventPool.Get<T>();
        ev.Owner = owner;
        initializer(ev);
        return ev;
    }
}