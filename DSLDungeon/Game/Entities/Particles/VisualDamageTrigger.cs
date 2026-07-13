using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Particles;

public readonly record struct VisualDamageTrigger(HexCoords Coords, string Text = "", string Type = "Damage");