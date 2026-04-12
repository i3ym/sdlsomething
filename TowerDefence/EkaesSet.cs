namespace SdlSomething.TowerDefence;

public interface IEkaesSet
{
    void Remove(Entity entity);
}
public sealed partial class EkaesSet<T> : IEkaesSet
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
    public bool TryGetRef(Entity entity, [MaybeNullWhen(false)] ref T value)
    {
        var realId = Sparse[entity.Id];
        if (realId == DefaultValue)
            return false;

        value = ref Dense[realId].Value;
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
public static partial class EkasSetExtensions
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

public static partial class EkasSetExtensions
{
    public static ZipEnumerator<T1, T2> Zip<T1, T2>(this EkaesSet<T1> t1, EkaesSet<T2> t2)
        where T1 : unmanaged
        where T2 : unmanaged =>
        new(t1, t2);

    public ref struct ZipEnumerator<T1, T2>
        where T1 : unmanaged
        where T2 : unmanaged
    {
        public Output Current => new(Current1.Entity, ref Current1.Value, ref Current2);
        public ref EkaesSet<T1>.WithHeader Current1;
        public ref T2 Current2;

        Span<EkaesSet<T1>.WithHeader>.Enumerator Enumerator1;
        readonly EkaesSet<T2> Set2;

        public ZipEnumerator(EkaesSet<T1> t1, EkaesSet<T2> t2)
        {
            Enumerator1 = t1.GetEnumerator();
            Set2 = t2;
        }

        public bool MoveNext()
        {
            if (!Enumerator1.MoveNext())
                return false;

            Current1 = ref Enumerator1.Current;
            if (!Set2.TryGetRef(Current1.Entity, ref Current2))
                return false;

            return true;
        }

        [UnscopedRef]
        public ref ZipEnumerator<T1, T2> GetEnumerator() => ref this;


        public readonly ref struct Output
        {
            public readonly Entity Entity;
            public readonly ref T1 Current1;
            public readonly ref T2 Current2;

            public Output(Entity entity, ref T1 current1, ref T2 current2)
            {
                Entity = entity;
                Current1 = ref current1;
                Current2 = ref current2;
            }
        }
    }
}
