using System.Reflection;

namespace DSLDungeon.Game.DSL;

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

        try
        {
            tickMethod.Invoke(instance, new object[] { context });
        }
        catch (TargetInvocationException ex)
        {
            // Распаковываем исключение, чтобы сохранить стек вызовов и тип ошибки (например, TimeoutException)
            if (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
            throw;
        }
    }
}