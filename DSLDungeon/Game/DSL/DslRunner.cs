using System.Reflection;
using DSLDungeon.Game.DSL;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.DSL;

/// <summary>
/// Загружает и выполняет скрипт из Assembly.
/// </summary>
public static class DslRunner
{
    public static void Execute(Assembly assembly, DslContext context)
    {
        var scriptType = assembly.GetType("Script")
            ?? throw new InvalidOperationException("В Assembly не найден класс 'Script'.");

        var tickMethod = scriptType.GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(DslContext) }, null)
            ?? throw new InvalidOperationException("Класс 'Script' не реализует метод 'Tick(DslContext)'.");

        var instance = Activator.CreateInstance(scriptType)
            ?? throw new InvalidOperationException("Не удалось создать экземпляр 'Script'.");

        tickMethod.Invoke(instance, new object[] { context });
    }
}
