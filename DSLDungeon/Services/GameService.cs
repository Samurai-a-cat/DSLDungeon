using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public GameUiAgent UiAgent { get; } = new();

    private bool _isRunning;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _isRunning;

    public GameService()
    {
        // 1. Оптимальный размер карты для 150 крипов: 100x100 (10 000 тайлов) - грузится за 50мс!
        var map = new HexMap(100, 100);
        World = new WorldState(map);
        Loop = new GameLoop(World);

        // 2. Спавним Рыцаря точно по центру (50, 50)
        var heroId = EntityIdGenerator.Next();
        var hero = new Hero(heroId, "Рыцарь", new HexCoords(50, 50), World, maxHp: 2000);
        
        var sword = new Weapon("Меч правосудия", damage: 100, range: 1, attackSpeed: 0.15f, isRanged: false);
        hero.Inventory!.EquippedWeapon = sword;
    
        if (hero.Health != null)
        {
            hero.Health.RegenRate = 50; 
        }
        World.SpawnEntity(hero);

        // 3. Спавним 150 орков
        SpawnEnemyHorde(targetCount: 150);

        UiAgent.SyncFromGame(World, 0f);
    }

    private void SpawnEnemyHorde(int targetCount)
    {
        var random = new Random();
        
        var passableTiles = World.Map.GetAllTiles()
            .Where(t => t.IsPassable)
            .ToList();

        int spawnedCount = 0;

        while (spawnedCount < targetCount && passableTiles.Count > 0)
        {
            int index = random.Next(passableTiles.Count);
            var tile = passableTiles[index];
            passableTiles.RemoveAt(index);

            // Исключаем центральную клетку героя (50, 50)
            if (tile.Coords.Q == 50 && tile.Coords.R == 50)
                continue;

            var orcId = EntityIdGenerator.Next();
            var orc = new Orc(orcId, $"Орк #{spawnedCount + 1}", tile.Coords, World, maxHp: 40);
            var dagger = new Weapon("Ржавый кинжал", damage: 3, range: 1, attackSpeed: 1.0f, isRanged: false);
            orc.Inventory!.EquippedWeapon = dagger;

            World.SpawnEntity(orc);
            spawnedCount++;
        }

        World.AddLog($"[Режиссер] В бескрайних землях материализовалась орда из {spawnedCount} орков!");
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();

        Loop.Time.Initialize();

        _ = RunLoopAsync(_cts.Token);
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));

        try
        {
            while (_isRunning && !token.IsCancellationRequested && await timer.WaitForNextTickAsync(token))
            {
                Loop.GameTimeChannel.TimeScale = UiAgent.PendingSpeed;

                Loop.Update();

                float dt = Loop.GameTimeChannel.Delta.Value;
                UiAgent.SyncFromGame(World, dt);
            }
        }
        catch (OperationCanceledException) { }
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