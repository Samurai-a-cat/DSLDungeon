using DSLDungeon.Game.Entities.Components;

namespace DSLDungeon.Game.Entities.Combat;

/// <summary>
/// Единая точка правды для временных боевых состояний.
/// Всё, что живёт только между ударами — здесь, а не в StatSheet.
/// </summary>
public class CombatStateComponent : EntityComponent
{
    // === Импульс ===
    public bool IsImpulseActive { get; set; }
    public float ImpulseBonus { get; set; }

    // === Комбо ===
    public int ComboCount { get; set; }
    public Entity? ComboTarget { get; set; }

    // === Геометрия (заглушки для будущего) ===
    public bool IsBackstab { get; set; }
    public bool HasHeightAdvantage { get; set; }

    public void Reset()
    {
        IsImpulseActive = false;
        ImpulseBonus = 0;
        ComboCount = 0;
        ComboTarget = null;
        IsBackstab = false;
        HasHeightAdvantage = false;
    }
}