namespace DSLDungeon.Game.Entities.Components;

/// <summary>
/// Данные пассивной способности "Ярость берсерка".
/// Логика в BerserkRageProcess.
/// </summary>
public class BerserkRageData : EntityComponent
{
    public float HpThresholdPercent { get; set; } = 0.5f;
    public float DamageBonusPerMissingHp { get; set; } = 0.02f;
}
