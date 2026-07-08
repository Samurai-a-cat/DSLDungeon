using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public class ActionQueue
{
    private readonly WorldState _world;
    private readonly Queue<ActionCommand> _commands = new();
    private const int MaxQueueCapacity = 10;

    public ActionCommand? Current { get; private set; }
    public bool IsEmpty => Current == null && _commands.Count == 0;

    public ActionQueue(WorldState world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public bool Enqueue(ActionCommand command)
    {
        if (_commands.Count >= MaxQueueCapacity)
        {
            Console.WriteLine("Переполнена очередь команд!");
            command.Release();
            return false;
        }
        
        _commands.Enqueue(command);
        return true;
    }

    public void Clear()
    {
        if (Current != null)
        {
            if (!Current.IsUninterruptible)
            {
                SafeExecute(Current, cmd => 
                {
                    cmd.Cancel();
                    cmd.OnCancel(_world);
                });
                Current.Release();
                Current = null;
            }
        }
        Flush();
    }
    
    /// <summary>
    /// Очищает только очередь ожидающих команд, не прерывая текущее выполняемое действие.
    /// </summary>
    public void Flush()
    {
        if (_commands.Count == 0) return;

        // Временный список для команд, которые нельзя удалять
        List<ActionCommand>? keptCommands = null; 

        while (_commands.Count > 0)
        {
            var cmd = _commands.Dequeue();
            
            if (cmd.IsUninterruptible)
            {
                // Сохраняем, чтобы вернуть в очередь позже
                keptCommands ??= new List<ActionCommand>(2); // Максимальная емкость у нас 10, аллокации минимальны
                keptCommands.Add(cmd);
            }
            else
            {
                // Уничтожаем обычные команды
                SafeExecute(cmd, command => 
                {
                    command.Cancel();
                    command.OnCancel(_world);
                });
                cmd.Release();
            }
        }

        // Возвращаем неуязвимые команды обратно в очередь
        if (keptCommands != null)
        {
            foreach (var cmd in keptCommands)
                _commands.Enqueue(cmd);
        }
    }

    public void Update(float deltaTime)
    {
        if (Current == null)
        {
            if (_commands.Count == 0) return;
            Current = _commands.Dequeue();
            
            // Если OnStart упадет, мы не дадим игре крашнуться
            SafeExecute(Current, cmd => cmd.OnStart(_world));
        }

        if (Current == null) return; 

        // Тикаем
        SafeExecute(Current, cmd => cmd.Tick(deltaTime, _world));

        if (!Current.IsFinished) return;

        var finished = Current;
        Current = null;

        try
        {
            if (finished.IsCancelled)
                finished.OnCancel(_world);
            else
                finished.OnFinish(_world);
        }
        catch (Exception ex)
        {
            // Логируем ошибку скрипта, но не крашим игру
            Console.WriteLine($"[ActionQueue] Error in {finished.GetType().Name}: {ex.Message}");
        }
        finally 
        { 
            finished.Release(); 
        }
    }

    /// <summary>
    /// Безопасно выполняет действие над командой, перехватывая любые исключения.
    /// </summary>
    private void SafeExecute(ActionCommand cmd, Action<ActionCommand> action)
    {
        try
        {
            action(cmd);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActionQueue] Script error in {cmd.GetType().Name}: {ex.Message}");
            cmd.Cancel();
        }
    }
}