using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DSLDungeon.Game.Core.Time;

/// <summary>
/// Система управления игровым временем. Контролирует каналы и рассчитывает глобальный DeltaTime.
/// </summary>
/// <remarks>
/// Рекомендуется использовать статические каналы или предварительно выделенный массив 
/// для избежания аллокаций в цикле обновления.
/// </remarks>
/// <example>
/// Инициализация и использование:
/// <code>
/// TimeSystem.Initialize();
/// TimeChannel worldTime = TimeSystem.CreateChannel();
/// TimeChannel uiTime = TimeSystem.CreateChannel();
/// </code>
/// В игровом цикле:
/// <code>
/// TimeSystem.Update();
/// 
/// float dt = worldTime.Delta.Value;
/// </code>
/// </example>
public static class TimeSystem
{
    private static long _lastTimestamp;
    private static double _maxDeltaBarrier = 0.1; // По умолчанию 100 мс
    private static readonly double TicksToSeconds = 1.0 / Stopwatch.Frequency;

    // Ограничиваем количество каналов фиксированным массивом для избежания аллокаций при итерации
    private static readonly TimeChannel[] _channels = new TimeChannel[16];
    private static int _channelCount;

    /// <summary>
    /// Максимальный порог для одного кадра (в секундах). 
    /// Предотвращает скачки физики при потере фокуса вкладки браузера.
    /// </summary>
    public static double MaxDeltaBarrier
    {
        get => _maxDeltaBarrier;
        set => _maxDeltaBarrier = value;
    }

    /// <summary>
    /// Инициализирует или сбрасывает точку отсчета времени.
    /// </summary>
    public static void Initialize()
    {
        _lastTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Создает и регистрирует новый канал времени.
    /// </summary>
    public static TimeChannel CreateChannel()
    {
        if (_channelCount >= _channels.Length)
        {
            throw new InvalidOperationException("Достигнут лимит каналов времени.");
        }

        var channel = new TimeChannel();
        _channels[_channelCount++] = channel;
        return channel;
    }

    /// <summary>
    /// Обновляет состояние всех каналов времени на основе системного таймера.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update()
    {
        long currentTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = currentTimestamp - _lastTimestamp;
        _lastTimestamp = currentTimestamp;

        double rawDelta = elapsedTicks * TicksToSeconds;

        // Защита от больших скачков времени (например, если вкладка Blazor была неактивна)
        if (rawDelta > _maxDeltaBarrier)
        {
            rawDelta = _maxDeltaBarrier;
        }

        float rawDeltaFloat = (float)rawDelta;

        // Прямой проход по массиву без использования IEnumerator для исключения аллокаций в куче
        int count = _channelCount;
        TimeChannel[] channels = _channels;
        for (int i = 0; i < count; i++)
        {
            channels[i].Update(rawDeltaFloat);
        }
    }
}