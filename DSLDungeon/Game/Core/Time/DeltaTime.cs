namespace DSLDungeon.Game.Core.Time;

/// <summary>
/// Представляет снимок изменения времени за текущий кадр.
/// </summary>
public readonly record struct DeltaTime(float Value, float RawValue)
{
    public float ValueMs => Value * 1000f;
    public float RawValueMs => RawValue * 1000f;

    public static implicit operator float(DeltaTime dt) => dt.Value;
}
