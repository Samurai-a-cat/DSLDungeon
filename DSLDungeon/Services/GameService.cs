using DSLDungeon.Game;
using DSLDungeon.Game.Core;
using DSLDungeon.Game.Entities;
using DSLDungeon.Game.Entities.Characters;
using DSLDungeon.Game.Entities.Components;
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
        var map = new HexMap(7, 7);
        World = new WorldState(map);
        Loop = new GameLoop(World);

        var heroId = EntityIdGenerator.Next();
        var hero = HeroFactory.CreateKnight(heroId, new HexCoords(1, 1), World);

        var sword = WeaponPresets.CreateSwordOfJustice();
        hero.GetComponent<EquipmentComponent>().Equip(EquipmentSlot.MainHand, sword);

        hero.GetComponent<HealthComponent>().RegenRate = 5;

        World.SpawnEntity(hero);

        var orcId = EntityIdGenerator.Next();
        var orc = OrcFactory.CreateChampion(orcId, "Орк-Чемпион Грумш", new HexCoords(4, 3), World);

        var axe = WeaponPresets.CreateOrcChampionAxe();
        orc.GetComponent<EquipmentComponent>().Equip(EquipmentSlot.MainHand, axe);

        World.SpawnEntity(orc);

        World.AddLog("╔══════════════════════════════════════════════════════════════╗");
        World.AddLog("║         ДУЭЛЬ: Рыцарь vs Орк-Чемпион Грумш                 ║");
        World.AddLog("╠══════════════════════════════════════════════════════════════╣");
        World.AddLog("║  Рыцарь:  Сила 15 | Ловк 10 | Инт 8  | Тел 12 | Меч правосудия  ║");
        World.AddLog("║  Грумш:   Сила 20 | Ловк 8  | Инт 4  | Тел 18 | Топор чемпиона  ║");
        World.AddLog("╚══════════════════════════════════════════════════════════════╝");

        UiAgent.SyncFromGame(World, 0f);
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
