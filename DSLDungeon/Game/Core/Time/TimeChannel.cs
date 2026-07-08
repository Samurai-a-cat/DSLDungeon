using System.Runtime.CompilerServices;

namespace DSLDungeon.Game.Core.Time;

/// <summary>
/// Представляет отдельный канал времени со своим масштабом (TimeScale).
/// </summary>
public sealed class TimeChannel
{
    private double _elapsed;
    private float _timeScale = 1.0f;
    private DeltaTime _currentDelta;

    /// <summary>
    /// Масштаб течения времени для данного канала. 
    /// Например: 0.5f — замедление в два раза, 0.0f — пауза.
    /// </summary>
    public float TimeScale
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _timeScale;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _timeScale = value;
    }

    /// <summary>
    /// Текущее значение DeltaTime для данного кадра.
    /// </summary>
    public DeltaTime Delta
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _currentDelta;
    }

    /// <summary>
    /// Общее прошедшее время в секундах в рамках данного канала (с учетом масштабирования).
    /// </summary>
    public double Elapsed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _elapsed;
    }

    internal TimeChannel() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update(float rawDeltaSeconds)
    {
        float scaledDelta = rawDeltaSeconds * _timeScale;
        _currentDelta = new DeltaTime(scaledDelta, rawDeltaSeconds);
        _elapsed += scaledDelta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _elapsed = 0.0;
        _currentDelta = default;
    }
}