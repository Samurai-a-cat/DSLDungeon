namespace DSLDungeon.Game.Entities.Items;

public class InventorySystem(WorldState world)
{
    public List<Item> Items { get; } = new();

    public Weapon? EquippedWeapon { get; set; }
}