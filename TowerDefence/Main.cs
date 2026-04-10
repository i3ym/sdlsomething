using System.Collections.Frozen;
using System.Drawing;
using System.Text;

namespace SdlSomething.TowerDefence;

public sealed class Main
{
    public Ekaes World { get; }

    public Main()
    {
        World = new Ekaes([
            .. EnemySystem.GetRegistrations(),
            WorldTypeRegistration.From<TowerPosition>(),
        ]);

        World.Entity()
            .Set(new EnemyHealth(1))
            .Set(new EnemyPosition(0, 3));
        World.Entity()
            .Set(new EnemyHealth(1))
            .Set(new EnemyPosition(0, 20));

        World.Entity()
            .Set(new TowerPosition(0, 0));
    }

    public void FixedTick(int tick)
    {
        EnemySystem.FixedTick(World, tick);
        if (tick % 1 == 0)
            towersAttack();


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

        if (health.Health <= 0)
            entity.Destroy();
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

public readonly record struct Entity(int Id)
{
    public FatEntity Fat<T>(EkaesSet<T> set) where T : unmanaged => Fat(set.World);
    public FatEntity Fat(Ekaes world) => new(this, world);
}
public readonly record struct FatEntity(Entity Entity, Ekaes World)
{
    public static implicit operator Entity(FatEntity entity) => entity.Entity;
}

public readonly record struct WorldTypeRegistration(string Id, Type Type)
{
    public static WorldTypeRegistration From<T>()
        where T : unmanaged =>
        new(typeof(T).Name, typeof(T));
}
public sealed class Ekaes
{
    readonly FrozenDictionary<Type, string> TypeCache;
    readonly (string, IEkaesSet)[] Components;
    readonly Queue<int> DestroyedEntities = [];
    int MaxEntityId = 0;

    public Ekaes(IReadOnlyCollection<WorldTypeRegistration> sets)
    {
        TypeCache = sets.ToFrozenDictionary(c => c.Type, c => c.Id);
        Components = [.. sets.Select(c => (c.Id, (IEkaesSet) Activator.CreateInstance(typeof(EkaesSet<>).MakeGenericType(c.Type), [this])!))];
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
        if (Sparse.Length < length)
            Array.Resize(ref Sparse, BytesExtensions.EnsureArrayLength(256, Sparse.Length, length));

        Sparse.AsSpan(startingLength).Fill(DefaultValue);
    }
    void EnsureDenseCapacity(int length)
    {
        if (Dense.Length < length)
            Array.Resize(ref Dense, BytesExtensions.EnsureArrayLength(64, Dense.Length, length));
    }

    public ref T Get(Entity entity)
    {
        var realId = Sparse[entity.Id];
        return ref Dense[realId].Value;
    }
    public bool Has(Entity entity) => Sparse[entity.Id] != DefaultValue;
    public bool TryGet(Entity entity, [MaybeNullWhen(false)] out T value)
    {
        var realId = Sparse[entity.Id];
        if (realId == DefaultValue)
        {
            value = default;
            return false;
        }

        value = Dense[realId].Value;
        return false;
    }

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
        if (entity.Id >= Sparse.Length) return;

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


readonly struct BlockAllocator : IDisposable
{
    public static void Test()
    {
        var allocator = New();
        ;

        // var block = allocator.GetOrClaimBlock("asus", 1024);

        var arr = new BlockAllocatorArrayDense<int>(allocator, "sus", 3);
        arr.Test();

        foreach (ref var item in arr)
        {
            Console.WriteLine($"item: {item}");
        }
        ;
    }
    public static BlockAllocator New()
    {
        var allocator = new BlockAllocator(NativeArr.Allocate(1024 * 1024));
        allocator.Header.LastAllocated = allocator.DataStart;
        return allocator;
    }

    readonly Dictionary<string, BlockHeaderLocation> BlockCache = [];

    readonly NativeArr Array;
    readonly unsafe int FullHeaderSize = sizeof(FullHeader);
    readonly ref FullHeader Header => ref Ref<FullHeader>(Array.Slice(0, FullHeaderSize));
    readonly unsafe int BlockHeaderSize = 1024 * sizeof(BlockHeader);
    readonly Span<BlockHeader> BlockHeadersSpan => Array.Slice(FullHeaderSize, BlockHeaderSize).Cast<BlockHeader>();
    readonly int DataStart => FullHeaderSize + BlockHeaderSize;

    public BlockAllocator(NativeArr array) => Array = array;

    public unsafe long ClaimUnnamed<T>(int count)
        where T : unmanaged =>
        ClaimUnnamed(count * sizeof(T));
    public long ClaimUnnamed(int size)
    {
        var start = Header.LastAllocated;
        Header.LastAllocated += size;
        return start;
    }

    public unsafe Span<T> GetBlock<T>(long position, int count)
        where T : unmanaged =>
        Array.Slice(position, count * sizeof(T)).Cast<T>();
    public Span<byte> GetBlock(long position, int size) => Array.Slice(position, size);

    public ref T GetOrClaimRef<T>(string id)
        where T : unmanaged =>
        ref GetOrClaimBlock<T>(id, 1)[0];

    public unsafe Span<T> GetOrClaimBlock<T>(string id, int count)
        where T : unmanaged =>
        GetOrClaimBlock(id, count * sizeof(T)).Cast<T>();
    public Span<byte> GetOrClaimBlock(string id, int size)
    {
        if (!BlockCache.TryGetValue(id, out var location))
        {
            var pos = Header.LastAllocated;
            var end = pos + size;
            Header.LastAllocated = end;

            var header = new BlockHeader()
            {
                Location = new() { Position = pos, Size = size },
                Name = new String64(id),
            };
            BlockHeadersSpan[Header.BlockCount] = header;
            Header.BlockCount++;

            BlockCache[id] = location = header.Location;
        }

        return Array.Slice(location.Position, location.Size);
    }

    static ref T Ref<T>(Span<byte> span)
        where T : unmanaged =>
        ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));

    public void Dispose() => Array.Dispose();


    struct FullHeader
    {
        public long LastAllocated;
        public int BlockCount;
    }
    struct BlockHeader
    {
        public String64 Name;
        public BlockHeaderLocation Location;
    }
    struct BlockHeaderLocation
    {
        public long Position;
        public int Size;
    }

    [DebuggerDisplay("{DebugString}")]
    public struct String64
    {
        string DebugString => Encoding.UTF8.GetString(Span.TrimEnd((byte) '\0'));
        public Span<byte> Span => MemoryMarshal.CreateSpan(ref Chars, 1).Bytes();
        Bytes64 Chars;

        public String64(string str) => Encoding.UTF8.GetBytes(str, Span);
    }
    readonly struct Bytes32 { readonly Int128 A, B; }
    readonly struct Bytes64 { readonly Bytes32 A, B; }

    public readonly struct BSpan
    {
        readonly BlockAllocator Allocator;
        public readonly long Start;
        public readonly int Length;

        public BSpan(BlockAllocator allocator, long start, int length)
        {
            Allocator = allocator;
            Start = start;
            Length = length;
        }

        public Span<byte> AsSpan() => Allocator.Array.Slice(Start, Length);
    }

    public readonly unsafe struct NativeArr : IDisposable
    {
        public static NativeArr Allocate(long size)
        {
            return new NativeArr()
            {
                DataStartPointerValue = (nint) NativeMemory.AllocZeroed((nuint) size),
                Length = size,
            };
        }

        readonly nint PointerToDataPointer;

        readonly ref nint DataStartPointerValue => ref Ref<nint>(new Span<byte>((void*) PointerToDataPointer, sizeof(nint)));
        readonly ref long Length => ref Ref<long>(new Span<byte>((void*) DataStartPointerValue, sizeof(long)));
        readonly void* Data => (byte*) DataStartPointerValue + sizeof(long);

        public NativeArr() => PointerToDataPointer = (nint) NativeMemory.AllocZeroed((uint) sizeof(nint));

        public Span<byte> Slice(long start, int length)
        {
            var end = start + length + sizeof(long);
            if (Length < end)
            {
                length = checked((int) BytesExtensions.EnsureArrayLength(1024 * 1024, Length, end));
                var prevLen = Length;
                DataStartPointerValue = (nint) NativeMemory.Realloc((void*) DataStartPointerValue, (nuint) length);

                NativeMemory.Clear((byte*) DataStartPointerValue + prevLen, (nuint) (length - prevLen));
                Length = length;
            }

            return MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.AsRef<byte>(Data), (nint) start), length);
        }

        public void Dispose()
        {
            if (DataStartPointerValue != 0)
                NativeMemory.Free((void*) DataStartPointerValue);
            NativeMemory.Free(Data);
        }
    }
}
readonly unsafe struct BlockAllocatorArray<T>
    where T : unmanaged
{
    public void Test()
    {
        var fb = GetFirstBlock();

        ref var f = ref GetFooterBySpan(fb);
        f.NextBlock = unchecked((long) 0xFFFFFFFFFFFFFFFF);

        var items = BlockAsItems(fb);

        items[0] = (T) (object) 1;
        items[1] = (T) (object) 3;
        items[2] = (T) (object) 2;

        ;
    }

    readonly BlockAllocator Allocator;
    readonly string Id;
    readonly int BlockItemCount;
    readonly int BlockSizeBytes;
    readonly int BlockSizeBytesWithFooter;
    readonly ref Header HeaderData => ref Allocator.GetOrClaimRef<Header>(Id);

    public BlockAllocatorArray(BlockAllocator allocator, string id, int blockItemCount)
    {
        Allocator = allocator;
        Id = id;

        BlockItemCount = blockItemCount;
        BlockSizeBytes = blockItemCount * sizeof(T);
        BlockSizeBytesWithFooter = BlockSizeBytes + sizeof(Footer);
    }

    Span<byte> GetFirstBlock()
    {
        ref var pos = ref HeaderData.FirstBlockPos;
        if (pos == 0)
            pos = Allocator.ClaimUnnamed(BlockSizeBytesWithFooter);

        return Allocator.GetBlock(pos, BlockSizeBytesWithFooter);
    }
    Span<T> BlockAsItems(Span<byte> block) => block[..BlockSizeBytes].Cast<T>();

    ref Footer GetFooterBySpan(Span<byte> span) => ref Ref<Footer>(span[BlockSizeBytes..]);

    static ref TRef Ref<TRef>(Span<byte> span)
        where TRef : unmanaged =>
        ref Unsafe.As<byte, TRef>(ref MemoryMarshal.GetReference(span));

    public Enumerator GetEnumerator() => new(this);


    struct Header
    {
        public long FirstBlockPos;
        // public int ItemCount;
    }
    struct Footer
    {
        public long NextBlock;
    }

    public ref struct Enumerator
    {
        public readonly ref T Current => ref CurrentSpan.Cast<T>()[IndexInSpan];
        readonly BlockAllocatorArray<T> Allocator;
        Span<byte> CurrentSpan;
        int CountLeft;
        int IndexInSpan;

        public Enumerator(BlockAllocatorArray<T> allocator)
        {
            Allocator = allocator;

            CurrentSpan = allocator.GetFirstBlock();
            // CountLeft = allocator.HeaderData.ItemCount;
        }

        public bool MoveNext()
        {
            if (CountLeft == 0) return false;

            CountLeft--;
            IndexInSpan++;
            if (IndexInSpan == Allocator.BlockItemCount)
            {
                var next = Allocator.GetFooterBySpan(CurrentSpan).NextBlock;
                CurrentSpan = Allocator.Allocator.GetBlock(next, Allocator.BlockSizeBytesWithFooter);

                IndexInSpan = 0;
            }

            return true;
        }
    }
}

readonly unsafe struct BlockAllocatorArrayDense<T>
    where T : unmanaged
{
    public void Test()
    {
        var items = Items;

        items[0] = (T) (object) 1;
        items[1] = (T) (object) 3;
        items[2] = (T) (object) 2;

        ;
    }

    readonly BlockAllocator Allocator;
    readonly string Id;
    readonly ref int ItemCount => ref HeaderData.ItemCount;
    readonly Span<byte> RawSpan => Allocator.GetOrClaimBlock(Id, sizeof(Header) + (ItemCount * sizeof(T)));
    readonly ref Header HeaderData => ref Ref<Header>(Allocator.GetOrClaimBlock(Id, sizeof(Header)));
    public readonly Span<T> Items => RawSpan[sizeof(Header)..].Cast<T>();

    public BlockAllocatorArrayDense(BlockAllocator allocator, string id, int initialCount)
    {
        Allocator = allocator;
        Id = id;

        allocator.GetOrClaimBlock(Id, sizeof(Header) + initialCount);
        HeaderData.ItemCount = initialCount;
    }

    static ref TRef Ref<TRef>(Span<byte> span)
        where TRef : unmanaged =>
        ref Unsafe.As<byte, TRef>(ref MemoryMarshal.GetReference(span));

    public Span<T>.Enumerator GetEnumerator() => Items.GetEnumerator();


    struct Header
    {
        public int ItemCount;
    }
}
