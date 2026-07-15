using System.Reflection;
using DSLDungeon.Game.DSL;
using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Entities.Components;

/// <summary>
/// Компонент ИИ для героев. Вызывает DSL-скрипт раз в 500 мс, если очередь пуста.
/// </summary>
public class DslAiComponent : EntityComponent
{
    public Assembly? CompiledScript { get; set; }
    private float _lastRunTime;
    private const float RunInterval = 0.5f;

    public override void OnUpdate(float deltaTime)
    {
        if (CompiledScript == null || Owner is not Actor actor) return;
        if (actor.World == null) return;

        // DSL-скрипт срабатывает только когда очередь пуста (Idle State)
        if (!actor.Queue.IsEmpty) return;

        _lastRunTime += deltaTime;
        if (_lastRunTime < RunInterval) return;
        _lastRunTime = 0;

        try
        {
            var context = new DslContext(actor, actor.World);
            DslRunner.Execute(CompiledScript, context);
        }
        catch (Exception ex)
        {
            actor.World.AddLog($"[DSL ERROR] {actor.Name}: {ex.Message}");
        }
    }
}
