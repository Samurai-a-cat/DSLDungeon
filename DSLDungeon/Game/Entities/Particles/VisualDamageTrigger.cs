using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Particles;

public class VisualDamageTrigger
{
    public HexCoords Coords { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "Damage";
}