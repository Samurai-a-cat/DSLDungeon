using System.Reflection;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public class SystemsRegistry
{
    private readonly List<IGameSystem> _orderedSystems = new();
    private readonly Dictionary<Type, IGameSystem> _systemsByType = new();

    public void Initialize()
    {
        _orderedSystems.Clear();
        _systemsByType.Clear();

        // 1. Находим все неабстрактные классы, реализующие IGameSystem
        var systemTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IGameSystem).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

        var systemsWithOrder = new List<(IGameSystem System, int Order)>();

        foreach (var type in systemTypes)
        {
            // Получаем атрибут порядка (если нет, по умолчанию даем 100)
            var orderAttr = type.GetCustomAttribute<SystemOrderAttribute>();
            int order = orderAttr?.Order ?? 100;

            try
            {
                var systemInstance = (IGameSystem)Activator.CreateInstance(type)!;
                systemsWithOrder.Add((systemInstance, order));
                _systemsByType[type] = systemInstance;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось создать систему {type.Name}: {ex.Message}");
            }
        }

        // 2. Сортируем системы по возрастанию приоритета (чем меньше число, тем раньше выполнится)
        var sorted = systemsWithOrder.OrderBy(s => s.Order).Select(s => s.System);
        _orderedSystems.AddRange(sorted);
    }

    /// <summary>
    /// Возвращает систему по её типу. Используется событиями для быстрой регистрации.
    /// </summary>
    public T Get<T>() where T : class, IGameSystem
    {
        if (_systemsByType.TryGetValue(typeof(T), out var system))
        {
            return (T)system;
        }
        throw new InvalidOperationException($"Система {typeof(T).Name} не зарегистрирована в реестре!");
    }

    /// <summary>
    /// Последовательно обновляет все системы в правильном порядке.
    /// </summary>
    public void Update(float deltaTime, WorldState world)
    {
        // Избегаем аллокаций при итерации
        int count = _orderedSystems.Count;
        for (int i = 0; i < count; i++)
        {
            _orderedSystems[i].Update(deltaTime, world);
        }
    }
}