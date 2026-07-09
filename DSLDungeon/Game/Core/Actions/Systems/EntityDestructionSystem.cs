using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions.Systems;

[SystemOrder(95)] // Выполняется в самом конце кадра, непосредственно перед фазой FinalizeDespawns
public class EntityDestructionSystem : IGameSystem
{
    public void Update(float deltaTime, WorldState world)
    {
        // Сканируем абсолютно все сущности в мире (и акторов, и статические объекты)
        foreach (var entity in world.GetAllEntities())
        {
            // Если у объекта есть компонент здоровья и он мертв — отправляем на деспавн
            if (entity.Health is { IsDead: true })
            {
                world.Despawn(entity.Id);
            }
        }
    }
}