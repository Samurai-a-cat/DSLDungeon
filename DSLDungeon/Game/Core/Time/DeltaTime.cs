using System;
using System.Runtime.CompilerServices;

namespace DSLDungeon.Game.Core.Time;

/// <summary>
/// Представляет снимок изменения времени за текущий кадр.
/// </summary>
public readonly record struct DeltaTime : IEquatable<DeltaTime>
{
    /// <summary>
    /// Масштабированное приращение времени в секундах.
    /// </summary>
    public readonly float Value;

    /// <summary>
    /// Реальное (немасштабированное) приращение времени в секундах.
    /// </summary>
    public readonly float RawValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DeltaTime(float value, float rawValue)
    {
        Value = value;
        RawValue = rawValue;
    }

    /// <summary>
    /// Масштабированное время в миллисекундах.
    /// </summary>
    public float ValueMs => Value * 1000f;

    /// <summary>
    /// Реальное время в миллисекундах.
    /// </summary>
    public float RawValueMs => RawValue * 1000f;

    public static implicit operator float(DeltaTime dt) => dt.Value;
}