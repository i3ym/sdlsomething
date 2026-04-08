using System.Collections.Frozen;
using System.Text;

namespace SdlSomething.TowerDefence;

public sealed class Main
{
    public Ekaes World { get; }

    public Main()
    {
        World = new Ekaes(new()
        {
            ["towerPosition"] = typeof(TowerPosition),
            ["enemyPosition"] = typeof(EnemyPosition),
            ["enemyHealth"] = typeof(EnemyHealth),
        });

        World.Entity()
            .Set(new EnemyHealth(1, 0, 0))
            .Set(new EnemyPosition(0, 3));
        World.Entity()
            .Set(new EnemyHealth(1, 0, 0))
            .Set(new EnemyPosition(0, 20));

        World.Entity()
            .Set(new TowerPosition(0, 0));
    }

    int Frame = 0;
    public void Update()
    {
        Frame++;
        if (Frame % 10 == 0)
            enemiesSpawn();
        if (Frame % 5 == 0)
            enemiesMove();
        if (Frame % 1 == 0)
            towersAttack();


        void enemiesSpawn()
        {
            var angle = Random.Shared.NextDouble() * Math.PI * 2;
            var pos = new EnemyPosition(Fixed3.From(Math.Cos(angle)) * 20, Fixed3.From(Math.Sin(angle)) * 20);

            World.Entity()
                .Set(new EnemyHealth(1, 0, 0))
                .Set(pos);
        }
        void enemiesMove()
        {
            var enemyPositions = World.Component<EnemyPosition>();
            var towerPositions = World.Component<TowerPosition>();

            foreach (ref var enemy in enemyPositions)
            {
                ref var enemyPos = ref enemy.Value;
                TowerPosition? closest = null;
                var closestDist = Fixed3.MaxValue;

                foreach (ref var towerPos in towerPositions)
                {
                    var dist = towerPos.Value.DistanceSquared(enemyPos);
                    if (dist < closestDist)
                    {
                        closest = towerPos.Value;
                        closestDist = dist;
                    }
                }

                if (closest is not { } c) continue;


                var dx = c.X - enemyPos.X;
                var dy = c.Y - enemyPos.Y;
                var max = Fixed3.Abs( Fixed3.MaxMagnitude(dx, dy));

                var cx = dx / max;
                var cy = dy / max;
                var mult = Fixed3.From(.1f);
                enemyPos = new(enemyPos.X + cx * mult, enemyPos.Y + cy * mult);
            }
        }
        void towersAttack()
        {
            var enemyPositions = World.Component<EnemyPosition>();
            var towerPositions = World.Component<TowerPosition>();

            const int minDist = 4;

            foreach (ref var tp in towerPositions)
            {
                var towerPos = tp.Value;
                Entity? closest = null;
                var closestDist = Fixed3.MaxValue;

                foreach (ref var enemyPos in enemyPositions)
                {
                    var dist = enemyPos.Value.DistanceSquared(towerPos);
                    if (dist > minDist) continue;
                    if (dist < closestDist)
                    {
                        closest = enemyPos.Entity;
                        closestDist = dist;
                    }
                }

                if (closest is not { } c) continue;

                Damage(c.Fat(World), 1);
            }
        }
    }


    static void Damage(FatEntity entity, int amount)
    {
        ref var health = ref entity.Get<EnemyHealth>();
        health = health with { Health = health.Health - 1 };

        Console.WriteLine($"damaged {entity} by {amount}");

        if (health.Health <= 0)
        {
            Console.WriteLine($"is dead");
            entity.Destroy();
        }
    }
}

public interface IPosition
{
    Fixed3 X { get; }
    Fixed3 Y { get; }
}
public static class PositionExtensions
{
    public static Fixed3 DistanceSquared<T1, T2>(this T1 left, T2 right)
        where T1 : unmanaged, IPosition
        where T2 : unmanaged, IPosition
    {
        var deltaX = left.X - right.X;
        var deltaY = left.Y - right.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }
}

public readonly record struct Tower;
public readonly record struct TowerPosition(Fixed3 X, Fixed3 Y) : IPosition;

public readonly record struct TowerCanOperate;
public readonly record struct TowerDelayPerOperation(uint DelayTicks);

public readonly record struct DamagedBy(uint Amount);


public readonly struct Enemy;
public record struct EnemyPosition(Fixed3 X, Fixed3 Y) : IPosition;
public record struct EnemyHealth(int Health, int Armor, int Shield)
{
    public void Damage(int amount)
    {
        if (Health < 0)
        {
            Health = 0;
            return;
        }

        Health -= amount;
    }
}

sealed class GameData
{
    const int BlockSize = 10 * 1024;

    [Obsolete("DANGEROUS WHEN REALLOCATING")]
    Span<byte> Span => Array.Span;
    NativeArr Array;

    ref Header HeaderData => ref Ref<Header>(Span);
    readonly int HeaderEnd;

    Span<ComponentHeader> ComponentHeaders => Span[HeaderEnd..ComponentHeadersEnd].Cast<ComponentHeader>();
    readonly int ComponentHeadersEnd;

    readonly int DataStart;

    public unsafe GameData(Dictionary<string, Action> systems)
    {
        Array = NativeArr.Alloc(16 * 1024 * 1024);
        HeaderEnd = sizeof(Header);
        ComponentHeadersEnd = HeaderEnd + systems.Count * sizeof(ComponentHeader);
        DataStart = ComponentHeadersEnd;

        HeaderData.Version = 1;
        HeaderData.ComponentsCount = systems.Count;
        HeaderData.LastAllocatedIndex = DataStart;

        var i = -1;
        foreach (var (name, ctor) in systems)
        {
            i++;

            ref var ch = ref ComponentHeaders[i];
            Encoding.UTF8.GetBytes(name, ch.Name.Span);
        }
    }

    static ref T Ref<T>(Span<byte> span)
        where T : unmanaged =>
        ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));

    public ForeignArray<T> GetComponent<T>(string system)
        where T : unmanaged
    {
        foreach (ref var header in ComponentHeaders)
            if (Encoding.UTF8.GetString(header.Name.Span.TrimEnd((byte) '\0')) == system)
                return new ForeignArray<T>(this, ref Unsafe.As<ComponentHeader, byte>(ref header));

        throw new KeyNotFoundException();
    }
    Span<T> ClaimBlock<T>(ref String64 system)
        where T : unmanaged
    {
        foreach (ref var header in ComponentHeaders)
        {
            if (header.Name.Span.SequenceEqual(system.Span))
            {
                var end = HeaderData.LastAllocatedIndex + BlockSize;
                var block = Span[HeaderData.LastAllocatedIndex..end];
                header.Buffers[header.BufferCount++] = HeaderData.LastAllocatedIndex;
                HeaderData.LastAllocatedIndex = end;

                return block.Cast<T>();
            }
        }

        throw new KeyNotFoundException();
    }
    public Span<T> ClaimBlock<T>(string system)
        where T : unmanaged
    {
        foreach (ref var header in ComponentHeaders)
        {
            if (Encoding.UTF8.GetString(header.Name.Span.TrimEnd((byte) '\0')) == system)
            {
                var end = HeaderData.LastAllocatedIndex + BlockSize;
                var block = Span[HeaderData.LastAllocatedIndex..end];
                header.Buffers[header.BufferCount++] = HeaderData.LastAllocatedIndex;
                HeaderData.LastAllocatedIndex = end;

                return block.Cast<T>();
            }
        }

        throw new KeyNotFoundException();
    }


    struct Header
    {
        public int Version;
        public int LastAllocatedIndex;
        public int ComponentsCount;
        int _Align1;
    }
    struct ComponentHeader
    {
        [UnscopedRef]
        public ref int BufferCount => ref Unsafe.As<Bytes64, int>(ref CountBuffersBacking);
        public Span<int> Buffers => MemoryMarshal.Cast<Bytes64, int>(MemoryMarshal.CreateSpan(ref CountBuffersBacking, 1))[1..];

        public String64 Name;
        Bytes64 CountBuffersBacking;
    }
    public readonly ref struct ForeignArray<T>
        where T : unmanaged
    {
        readonly GameData GameData;
        readonly Span<byte> FullData => GameData.Span;
        readonly ref ComponentHeader Header;

        internal ForeignArray(GameData gameData, ref byte header)
        {
            GameData = gameData;
            Header = ref Unsafe.As<byte, ComponentHeader>(ref header);
        }

        public Span<T> SubSpanFor(int spanIndex)
        {
            while (spanIndex >= Header.BufferCount)
                GameData.ClaimBlock<T>(ref Header.Name);

            return FullData.Slice(Header.Buffers[spanIndex], BlockSize).Cast<T>();
        }

        public ref T this[int index] => ref SubSpanFor(index / BlockSize)[index % BlockSize];


        public Enumerator GetEnumerator() => new(this);


        public ref struct Enumerator
        {
            readonly ForeignArray<T> Component;
            int SpanIndex = 0;
            int SpanPosition = -1;
            readonly Span<T> CurrentSpan => Component.SubSpanFor(SpanIndex);
            public readonly ref T Current => ref CurrentSpan[SpanPosition];

            public Enumerator(ForeignArray<T> component) => Component = component;

            public bool MoveNext()
            {
                SpanPosition++;
                if (SpanPosition >= CurrentSpan.Length)
                {
                    SpanPosition = 0;
                    SpanIndex++;
                }

                if (SpanIndex >= Component.Header.BufferCount)
                    return false;

                return true;
            }
        }
    }

    struct String64
    {
        public Span<byte> Span => MemoryMarshal.CreateSpan(ref Chars, 1).Bytes();
        Bytes64 Chars;
    }

    readonly struct Bytes32 { readonly Int128 A, B; }
    readonly struct Bytes64 { readonly Bytes32 A, B; }
    unsafe struct NativeArr
    {
        public static NativeArr Alloc(int length)
        {
            var ptr = NativeMemory.AlignedAlloc((nuint) length, (nuint) sizeof(nint));
            return new() { Data = ptr, Length = length };
        }

        public readonly Span<byte> Span => new Span<byte>(Data, Length);
        void* Data;
        int Length;

        public void Realloc(int length)
        {
            Data = NativeMemory.AlignedRealloc(Data, (nuint) length, (nuint) sizeof(nint));
            Length = length;
        }
    }
}

public readonly record struct Entity(int Id)
{
    public FatEntity Fat<T>(EkaesSet<T> set) where T : unmanaged => Fat(set.World);
    public FatEntity Fat(Ekaes world) => new(this, world);
}
public readonly record struct FatEntity(Entity Entity, Ekaes World)
{
    public static implicit operator Entity(FatEntity entity) => entity.Entity;
}
public sealed class Ekaes
{
    readonly FrozenDictionary<Type, string> TypeCache;
    readonly (string, IEkaesSet)[] Components;
    readonly Queue<int> DestroyedEntities = [];
    int MaxEntityId = 0;

    public Ekaes(Dictionary<string, Type> sets)
    {
        TypeCache = sets.ToFrozenDictionary(c => c.Value, c => c.Key);
        Components = [.. sets.Select(c => (c.Key, (IEkaesSet) Activator.CreateInstance(typeof(EkaesSet<>).MakeGenericType(c.Value), [this])!))];
    }

    public FatEntity Entity()
    {
        if (DestroyedEntities.TryDequeue(out var existing))
            return new FatEntity(new Entity(existing), this);

        return new FatEntity(new Entity(MaxEntityId++), this);
    }

    public void Destroy(Entity entity)
    {
        foreach (var (_, set) in Components)
            set.Remove(entity);

        DestroyedEntities.Enqueue(entity.Id);
    }

    public EkaesSet<T> Component<T>(string name)
        where T : unmanaged =>
        (EkaesSet<T>) Components.First(c => c.Item1 == name).Item2;
    public EkaesSet<T> Component<T>()
        where T : unmanaged =>
        Component<T>(TypeCache[typeof(T)]);
}
public interface IEkaesSet
{
    void Remove(Entity entity);
}
public sealed class EkaesSet<T> : IEkaesSet
    where T : unmanaged
{
    const int DefaultValue = -1;
    public Span<WithHeader> Items => Dense.AsSpan(0, Count);

    int[] Sparse = new int[256];
    WithHeader[] Dense = new WithHeader[64];
    public int Count { get; private set; } = 0;

    public Ekaes World { get; }

    public EkaesSet(Ekaes ekaes)
    {
        World = ekaes;
        Sparse.AsSpan().Fill(DefaultValue);
    }

    void EnsureSparseCapacity(int length)
    {
        var startingLength = Sparse.Length;
        while (Sparse.Length < length)
            Array.Resize(ref Sparse, Math.Max(Sparse.Length, 256) * 2);

        Sparse.AsSpan(startingLength).Fill(DefaultValue);
    }
    void EnsureDenseCapacity(int length)
    {
        while (Dense.Length < length)
            Array.Resize(ref Dense, Math.Max(Dense.Length, 64) * 2);
    }

    public ref T Get(Entity entity)
    {
        var realId = Sparse[entity.Id];
        return ref Dense[realId].Value;
    }
    public bool Has(Entity entity) => Sparse[entity.Id] != DefaultValue;

    public void Set(Entity entity, T value) => Set(entity, ref value);
    public void Set(Entity entity, ref T value)
    {
        EnsureSparseCapacity(entity.Id + 1);

        var realId = Sparse[entity.Id];
        if (realId != DefaultValue)
            Dense[realId] = new(entity, ref value);
        else
        {
            EnsureDenseCapacity(entity.Id + 1);

            Dense[Count] = new(entity, ref value);
            Sparse[entity.Id] = Count;
            Count++;
        }
    }
    public void Remove(Entity entity)
    {
        var realId = Sparse[entity.Id];
        if (realId == DefaultValue) return;

        Count--;
        if (Count == 0)
        {
            Sparse[entity.Id] = DefaultValue;
        }
        else
        {
            Dense[realId] = Dense[Count];
            Sparse[entity.Id] = DefaultValue;
            Sparse[Dense[Count].Entity.Id] = realId;
        }
    }


    // TODO: iterate backwards
    public Span<WithHeader>.Enumerator GetEnumerator() => Items.GetEnumerator();
    public FEnumerable FatEnumerable() => new(this);


    public readonly ref struct FEnumerable
    {
        readonly EkaesSet<T> Set;

        public FEnumerable(EkaesSet<T> set) => Set = set;

        public FEnumerator GetEnumerator() => new(Set);


        public ref struct FEnumerator
        {
            public FatWithHeader Current
            {
                get
                {
                    ref var current = ref Set.Dense[Position];
                    return new(current.Entity.Fat(Set.World), ref current.Value);
                }
            }
            readonly EkaesSet<T> Set;
            int Position = -1;

            public FEnumerator(EkaesSet<T> set) => Set = set;

            public bool MoveNext() => Position++ < Set.Count;
        }

        public ref struct FatWithHeader
        {
            public readonly FatEntity Entity;
            public ref T Value;

            public FatWithHeader(FatEntity entity, ref T value)
            {
                Entity = entity;
                Value = ref value;
            }
        }
    }
    public record struct WithHeader
    {
        public readonly Entity Entity;
        public T Value;

        public WithHeader(Entity entity, ref T value)
        {
            Entity = entity;
            Value = value;
        }
    }
}
public static class EkasSetExtensions
{
    public static Entity Set<T>(this Entity entity, EkaesSet<T> set, T value)
        where T : unmanaged =>
        entity.Set(set, ref value);

    public static Entity Set<T>(this Entity entity, EkaesSet<T> set, ref T value)
        where T : unmanaged
    {
        set.Set(entity, ref value);
        return entity;
    }

    public static ref T Get<T>(this Entity entity, EkaesSet<T> set)
        where T : unmanaged =>
        ref set.Get(entity);


    public static Entity Remove<T>(this Entity entity, EkaesSet<T> set)
        where T : unmanaged
    {
        set.Remove(entity);
        return entity;
    }

    public static void Destroy(this Entity entity, Ekaes world) => world.Destroy(entity);


    public static FatEntity Set<T>(this FatEntity entity, T value)
        where T : unmanaged =>
        entity.Set(ref value);

    public static FatEntity Set<T>(this FatEntity entity, ref T value)
        where T : unmanaged
    {
        entity.World.Component<T>().Set(entity, ref value);
        return entity;
    }

    public static bool Has<T>(this FatEntity entity)
        where T : unmanaged =>
        entity.World.Component<T>().Has(entity);
    public static ref T Get<T>(this FatEntity entity)
        where T : unmanaged =>
        ref entity.World.Component<T>().Get(entity);


    public static FatEntity Remove<T>(this FatEntity entity)
        where T : unmanaged
    {
        entity.World.Component<T>().Remove(entity);
        return entity;
    }

    public static void Destroy(this FatEntity entity) => entity.World.Destroy(entity);
}

public static class SpanExtensions
{
    public static Span<T> Cast<T>(this Span<byte> span)
        where T : unmanaged =>
        MemoryMarshal.Cast<byte, T>(span);

    public static ReadOnlySpan<T> Cast<T>(this ReadOnlySpan<byte> span)
        where T : unmanaged =>
        MemoryMarshal.Cast<byte, T>(span);

    public static Span<byte> Bytes<T>(this Span<T> span)
        where T : unmanaged =>
        MemoryMarshal.AsBytes(span);

    public static ReadOnlySpan<byte> Bytes<T>(this ReadOnlySpan<T> span)
        where T : unmanaged =>
        MemoryMarshal.AsBytes(span);
}


// class GData
// {
//     public static unsafe BlockAllocator New()
//     {
//         // getblockat 0


//         return allocator;
//     }
//     BlockAllocator Allocator;

//     ref Header HeaderData => ref Ref<Header>(Allocator);

//     public GData(BlockAllocator allocator)
//     {
//         Allocator = allocator;
//     }


//     static ref T Ref<T>(Span<byte> span)
//         where T : unmanaged =>
//         ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));


//     struct Header
//     {
//         public uint Version { get; init; }
//         public uint PageSize { get; init; }
//         public ulong NextFreeOffset;
//     }
// }
struct BlockAllocator
{
    public static unsafe BlockAllocator New()
    {
        var allocator = new BlockAllocator(NativeArr.Alloc(1 * 1024 * 1024))
        {
            HeaderData = new()
            {
                PageSize = 1024,
                NextFreeOffset = sizeof(Header),
            },
        };

        return allocator;
    }

    NativeArr Array;
    readonly unsafe ref Header HeaderData => ref Ref<Header>(Array.Slice(0, sizeof(Header)));

    public BlockAllocator(NativeArr array) => Array = array;

    [UnscopedRef]
    public BSpan NewBlock()
    {
        var start = HeaderData.NextFreeOffset;
        var end = start + HeaderData.PageSize;
        HeaderData.NextFreeOffset = end;

        return GetBlockAt(start);
    }
    [UnscopedRef]
    public BSpan GetBlockAt(nint position) => new BSpan(ref this, position, HeaderData.PageSize);


    static ref T Ref<T>(Span<byte> span)
        where T : unmanaged =>
        ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));


    struct Header
    {
        public int PageSize { get; init; }
        public nint NextFreeOffset;
    }

    public readonly ref struct BSpan
    {
        readonly ref BlockAllocator Allocator;
        public readonly nint Start;
        public readonly int Length;

        public BSpan(ref BlockAllocator allocator, nint start, int length)
        {
            Allocator = ref allocator;
            Start = start;
            Length = length;
        }

        public Span<byte> AsSpan() => Allocator.Array.Slice(Start, Length);
    }
    public unsafe struct NativeArr
    {
        public static NativeArr Alloc(nint length)
        {
            var ptr = NativeMemory.AlignedAlloc((nuint) length, (nuint) sizeof(nint));
            return new() { Data = ptr, Length = length };
        }


        void* Data;
        nint Length;

        public void Realloc(nint length)
        {
            Data = NativeMemory.AlignedRealloc(Data, (nuint) length, (nuint) sizeof(nint));
            Length = length;
        }

        public Span<byte> Slice(nint start, int length)
        {
            var end = start + length;
            while (end > Length)
                Realloc(Length * 2);

            return MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.AsRef<byte>(Data), start), length);
        }
    }
}
