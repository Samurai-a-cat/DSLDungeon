using System.Reflection;

namespace DSLDungeon.Game.Core.Actions;

public static class ActionPool
{
    private const int HardCap = 1000;

    private class PoolBucket
    {
        public readonly Queue<ActionCommand> Queue;

        public PoolBucket(int initialCapacity)
        {
            Queue = new Queue<ActionCommand>(initialCapacity);
        }
    }

    private static readonly Dictionary<Type, PoolBucket> _pools = new();

    public static void Initialize()
    {
        var commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ActionCommand)) && !t.IsAbstract);

        foreach (var type in commandTypes)
        {
            var config = type.GetCustomAttribute<PoolConfigAttribute>();
            if (config == null)
                throw new InvalidOperationException(
                    $"ОШИБКА ПУЛИНГА: Для команды '{type.Name}' не указан атрибут [PoolConfig].");

            var bucket = new PoolBucket(config.PreloadCount);
            _pools[type] = bucket;

            // === ПРОГРЕВ ПУЛА ===
            if (config.PreloadCount > 0)
            {
                for (int i = 0; i < config.PreloadCount; i++)
                {
                    try
                    {
                        var cmd = (ActionCommand)Activator.CreateInstance(type)!;
                        bucket.Queue.Enqueue(cmd);
                    }
                    catch (MissingMethodException)
                    {
                        throw new InvalidOperationException(
                            $"ОШИБКА ПУЛИНГА: У команды '{type.Name}' нет публичного конструктора без параметров.");
                    }
                }
            }
        }
    }

    public static T Get<T>() where T : ActionCommand, new()
    {
        if (!_pools.TryGetValue(typeof(T), out var bucket))
            throw new InvalidOperationException($"Пул для {typeof(T).Name} не инициализирован!");

        if (bucket.Queue.Count > 0)
        {
            var cmd = (T)bucket.Queue.Dequeue();
            cmd.MarkRented();
            return cmd;
        }

        var fresh = new T();
        fresh.MarkRented();
        return fresh;
    }

    internal static void ReturnInternal(ActionCommand command)
    {
        if (!_pools.TryGetValue(command.GetType(), out var bucket))
            throw new InvalidOperationException($"Пул для {command.GetType().Name} не инициализирован!");


        if (bucket.Queue.Count < HardCap)
        {
            command.Reset();
            bucket.Queue.Enqueue(command);
        }
    }
}