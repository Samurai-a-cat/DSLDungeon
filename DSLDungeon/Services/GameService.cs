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
    public WorldState World { get; private set; }
    public GameLoop Loop { get; private set; }
    
    // Наш промежуточный агент
    public GameUiAgent UiAgent { get; } = new();

    private bool _isRunning;
    private CancellationTokenSource? _cts;

    public GameService()
    {
        EventPool.Initialize();

        var map = new HexMap(3);
        World = new WorldState(map);
        Loop = new GameLoop(World);

        var hero = new Hero(new EntityId(1, 1), "Рыцарь", new HexCoords(0, 0), World, maxHp: 100);
        var sword = new Weapon("Стальной меч", damage: 15, range: 1, attackSpeed: 0.6f, isRanged: false);
        hero.Inventory!.EquippedWeapon = sword;

        var orc = new Orc(new EntityId(2, 1), "Орк", new HexCoords(2, 0), World, maxHp: 40);

        World.SpawnEntity(hero);
        World.SpawnEntity(orc);

        // Синхронизируем начальное состояние до старта
        UiAgent.SyncFromGame(World);
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        TimeSystem.Initialize();

        _ = RunLoopAsync(_cts.Token);
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));

        try
        {
            while (_isRunning && !token.IsCancellationRequested && await timer.WaitForNextTickAsync(token))
            {
                // 1. Считываем буфер ввода (безопасно применяем скорость)
                Loop.GameTimeChannel.TimeScale = UiAgent.PendingSpeed;

                // 2. Симуляция кадра игры
                Loop.Update();

                // 3. Заполняем буфер вывода (делаем снимок состояния)
                UiAgent.SyncFromGame(World);

                // 4. КООПЕРАТИВНЫЙ YIELD: явно отдаем квант времени браузеру.
                // Это предотвращает потоковое голодание JS и делает слайдер шелковистым.
                await Task.Delay(1, token); 
            }
        }
        catch (OperationCanceledException)
        {
            // Корректная остановка
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