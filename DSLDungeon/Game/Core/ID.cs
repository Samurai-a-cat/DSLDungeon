using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DSLDungeon.Game.Core;

/// <summary>
/// Универсальный и безопасный идентификатор сущности с поддержкой поколений (Generational ID).
/// Упакован в 32-битное число: 22 бита под индекс, 10 бит под поколение.
/// </summary>
/// <example>
/// 1. Проверка на пустоту
/// <code>
/// EntityId id = EntityId.None;
/// if (id.IsNone) { ... }
/// </code>
/// 2. Получение составных частей
/// <code>
/// uint index = id.Index;
/// uint generation = id.Generation;
/// </code>
/// 3. Сравнение идентификаторов
/// <code>
/// if (id1 > id2) { ... }
/// </code>
/// </example>
public readonly record struct EntityId(uint Value) : IComparable<EntityId>
{
    private const int GenerationBits = 10;
    private const uint IndexMask = (1 << (32 - GenerationBits)) - 1; // 0x003FFFFF (макс индекс: 4 194 303)
    private const uint GenerationMask = (1 << GenerationBits) - 1;   // 0x000003FF (макс поколение: 1023)

    public static readonly EntityId None = new(0);

    /// <summary>
    /// Индекс слота в памяти.
    /// </summary>
    public uint Index => Value & IndexMask;

    /// <summary>
    /// Поколение идентификатора для предотвращения проблемы ABA (устаревших ссылок).
    /// </summary>
    public uint Generation => Value >> (32 - GenerationBits);

    public bool IsNone => Value == 0;

    /// <summary>
    /// Конструктор для создания ID из индекса и поколения.
    /// </summary>
    public EntityId(uint index, uint generation) 
        : this(((generation & GenerationMask) << (32 - GenerationBits)) | (index & IndexMask)) {}

    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    public override string ToString() => IsNone ? "None" : $"#{Index}v{Generation}";
    
    public static bool operator <(EntityId left, EntityId right) => left.CompareTo(right) < 0;
    public static bool operator >(EntityId left, EntityId right) => left.CompareTo(right) > 0;
    public static bool operator <=(EntityId left, EntityId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(EntityId left, EntityId right) => left.CompareTo(right) >= 0;

    public static explicit operator EntityId(uint value) => new(value);
    public static explicit operator uint(EntityId id) => id.Value;
}

/// <summary>
/// Генератор уникальных ID с автоматическим переиспользованием освобожденных индексов.
/// </summary>
/// <remarks>
/// ID 0 зарезервирован за None. Новые ID начинают работу с поколения 1. <br/>
/// Примечание: Класс не является потокобезопасным (оптимизировано под Blazor WASM).
/// </remarks>
/// <example>
/// 1. Генерация нового ID (поколение начнется с 1)
/// <code> 
/// EntityId player = EntityIdGenerator.Next(); // Вернет, например, #1v1
/// </code>
/// 2. Освобождение ID при уничтожении объекта
/// <code>
/// EntityIdGenerator.Release(player);
/// </code>
/// 3. При следующем запросе вернется тот же индекс, но со следующим поколением
/// <code>
/// EntityId newPlayer = EntityIdGenerator.Next(); // Вернет #1v2
/// </code>
/// </example>
public static class EntityIdGenerator
{
    /// <remarks>
    /// Начинаем с поколения 1, а не 0. <br/>
    /// Причина: в C# массивы обнуляются при аллокации. <br/>
    /// Поколение 0 в данных = признак неинициализированного/освобожденного слота. <br/>
    /// Это дает бесплатную защиту от чтения мусора из переаллоцированных массивов.
    /// </remarks>>
    private static uint _nextId;
    private static readonly Queue<EntityId> _freeIds = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityId Next()
    {
        if (_freeIds.Count > 0)
            return _freeIds.Dequeue();

        _nextId = checked(_nextId + 1);
        return new EntityId(_nextId, 1);
    }

    /// <summary>
    /// Освобождает ID. Его индекс вернется в очередь, а поколение увеличится.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Release(EntityId id)
    {
        if (id.IsNone) return;
        uint nextGen = (id.Generation + 1) & 0x3FF; 
        _freeIds.Enqueue(new EntityId(id.Index, nextGen));
    }
}

/// <summary>
/// Оптимизированные методы расширения для работы с коллекциями идентификаторов.
/// </summary>
/// <example>
/// <code>
/// EntityId[] activeIds = [id1, id2];
/// </code>
/// 1. Быстрая проверка наличия без выделения памяти в куче
/// <code>
/// bool exists = activeIds.AsSpan().ContainsId(id1);
/// </code>
/// 2. Получение первого элемента или None
/// <code>
/// EntityId first = activeIds.AsSpan().FirstOrNone();
/// </code>
/// </example>
public static class EntityIdExtensions
{
    /// <summary>
    /// Возвращает первый ID из набора или EntityId.None, если набор пуст.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityId FirstOrNone(this ReadOnlySpan<EntityId> source)
    {
        return source.Length > 0 ? source[0] : EntityId.None;
    }

    /// <summary>
    /// Проверяет наличие ID в наборе без аллокаций.
    /// </summary>
    public static bool ContainsId(this ReadOnlySpan<EntityId> source, EntityId value)
    {
        foreach (var t in source)
            if (t == value)
                return true;
        return false;
    }
}