namespace DSLDungeon.Game.Entities.Items;

public class InventorySystem
{
    public List<Item> Items { get; } = new();
    public Weapon? EquippedWeapon { get; set; }
}
