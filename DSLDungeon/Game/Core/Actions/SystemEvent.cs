namespace DSLDungeon.Game.Core.Actions;

/// <summary>
/// Системное событие: движок создаёт и управляет им сам.
/// Никогда не бывает абилкой персонажа.
/// </summary>
public abstract class SystemEvent<TSystem> : QueueEvent<TSystem>
    where TSystem : class, IEntityTrackingSystem
{
}
