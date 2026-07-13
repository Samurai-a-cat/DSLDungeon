using DSLDungeon.Game.Entities.Items;

namespace DSLDungeon.Game.Entities.Components;

public class EquipmentComponent : EntityComponent
{
    private readonly Dictionary<EquipmentSlot, Item> _equipped = new();

    public IReadOnlyDictionary<EquipmentSlot, Item> Equipped => _equipped;

    public bool Equip(EquipmentSlot slot, Item item)
    {
        if (_equipped.TryGetValue(slot, out _))
        {
            Unequip(slot);
        }

        _equipped[slot] = item;

        var stats = Owner.GetComponent<StatsComponent>();
        foreach (var mod in item.GetModifiers())
        {
            stats.AddModifier(mod.Key, mod.Value);
        }

        return true;
    }

    public void Unequip(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out var item)) return;

        var stats = Owner.GetComponent<StatsComponent>();
        stats.RemoveModifiersByTag(item.Name);

        _equipped.Remove(slot);
    }
}