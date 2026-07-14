namespace DSLDungeon.Game.Entities.Components;

public abstract class EntityComponent
{
    public Entity Owner { get; private set; } = null!;

    public virtual void OnAttached(Entity owner) => Owner = owner;
    public virtual void OnDetached() { }
    public virtual void OnUpdate(float deltaTime) { }
}
