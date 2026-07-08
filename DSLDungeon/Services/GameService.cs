using DSLDungeon.Game;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Core.Time;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Characters;
using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Services;

public class GameService : IDisposable
{
    public float GameSpeed
    {
        get => Loop.GameTimeChannel.TimeScale;
        set => Loop.GameTimeChannel.TimeScale = value;
    }
    public WorldState World { get; private set; }
    public GameLoop Loop { get; private set; }
    
    private bool _isRunning;
    private CancellationTokenSource? _cts;

    // Событие, на которое подпишется Blazor-компонент для обновления экрана
    public event Action? OnGameTick;

    public GameService()
    {
        ActionPool.Initialize();

        // 1. Инициализируем мир и цикл
        var map = new HexMap(3);
        World = new WorldState(map);
        Loop = new GameLoop(World);

        // 2. Спавним демо-персонажей (Теперь передаем World!)
        var hero = new Hero(new EntityId(1, 1), "Рыцарь", new HexCoords(0, 0), World, maxHp: 100);
        var sword = new Weapon("Стальной меч", damage: 15, range: 1, attackSpeed: 0.6f, isRanged: false);
        
        // Инвентарь точно создан в базовом классе Actor, поэтому ! уместен.
        hero.Inventory!.EquippedWeapon = sword;

        var orc = new Orc(new EntityId(2, 1), "Орк", new HexCoords(2, 0), World, maxHp: 40);

        // 3. Регистрируем в мире
        World.SpawnEntity(hero);
        World.SpawnEntity(orc);
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();

        TimeSystem.Initialize(); // Сбрасываем системные часы перед началом

        // Запуск цикла в фоновом режиме (не блокирует поток отрисовки браузера)
        _ = RunLoopAsync(_cts.Token);
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        // Настраиваем таймер на ~60 тиков в секунду (16 миллисекунд)
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));

        try
        {
            while (_isRunning && !token.IsCancellationRequested && await timer.WaitForNextTickAsync(token))
            {
                // 1. Шаг симуляции игры
                Loop.Update();

                // 2. Оповещаем Blazor-интерфейс, что пора перерисовать экран
                OnGameTick?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное поведение при остановке
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
    

}