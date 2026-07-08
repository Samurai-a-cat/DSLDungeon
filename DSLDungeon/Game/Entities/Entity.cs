using DSLDungeon.Game.Core;
using DSLDungeon.Game.Entities.Systems;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public abstract class Entity(EntityId id, string name, HexCoords position)
{
    public EntityId Id { get; } = id;
    public string Name { get; set; } = name;
    public event Action<Entity>? OnDeath;
    public HexCoords Position { get; set; } = position;
    public HealthSystem? Health { get; protected set; }
    
    protected void InitializeHealth(int maxHp)
    {
        Health = new HealthSystem(maxHp);
        Health.OnDeath += HandleComponentDeath;
    }
    private void HandleComponentDeath()
    {
        OnDeath?.Invoke(this);
        Console.WriteLine($"[Смерть] {Name} был убит и больше не действует.");
        if (Health != null) Health.OnDeath -= HandleComponentDeath;
    }
}