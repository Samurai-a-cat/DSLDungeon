using System.Reflection;

namespace DSLDungeon.Game.Core.Actions;

public static class EventPool
{
    private const int HardCap = 1000;

    private class PoolBucket
    {
        public readonly Queue<IQueueEvent> Queue;

        public PoolBucket(int initialCapacity)
        {
            Queue = new Queue<IQueueEvent>(initialCapacity);
        }
    }

    private static readonly Dictionary<Type, PoolBucket> _pools = new();

    public static void Initialize()
    {
        var eventTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IQueueEvent).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

        foreach (var type in eventTypes)
        {
            var config = type.GetCustomAttribute<PoolConfigAttribute>();
            int preloadCount = config?.PreloadCount ?? 0;

            var bucket = new PoolBucket(preloadCount);
            _pools[type] = bucket;

            if (preloadCount > 0)
            {
                for (int i = 0; i < preloadCount; i++)
                {
                    try
                    {
                        var ev = (IQueueEvent)Activator.CreateInstance(type)!;
                        bucket.Queue.Enqueue(ev);
                    }
                    catch (MissingMethodException)
                    {
                        throw new InvalidOperationException(
                            $"ОШИБКА ПУЛИНГА: У события '{type.Name}' нет публичного конструктора без параметров.");
                    }
                }
            }
        }
    }

    public static T Get<T>() where T : class, IQueueEvent, new()
    {
        if (!_pools.TryGetValue(typeof(T), out var bucket))
            throw new InvalidOperationException($"Пул для {typeof(T).Name} не инициализирован!");

        if (bucket.Queue.Count > 0)
        {
            var ev = (T)bucket.Queue.Dequeue();
            return ev;
        }

        return new T();
    }

    internal static void Return(IQueueEvent ev)
    {
        if (!_pools.TryGetValue(ev.GetType(), out var bucket))
            throw new InvalidOperationException($"Пул для {ev.GetType().Name} не инициализирован!");

        if (bucket.Queue.Count < HardCap)
        {
            ev.Reset();
            bucket.Queue.Enqueue(ev);
        }
    }
}
