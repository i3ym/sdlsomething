namespace SdlSomething.TowerDefence;

public sealed class Main
{
    public Main()
    {
        // var systemExecutor = new SystemExecutor(World.Create());
        // var world = systemExecutor.World;

        // var e1 = world.Entity()
        //     .Set(new EnemyPosition(0, 0));
        // var e2 = world.Entity()
        //     .Set(new EnemyPosition(0, 1));

        // var t1 = world.Entity()
        //     .Add<TowerCanOperate>()
        //     .Set(new BasicTowerShootGroup.Data(1))
        //     .Set(new TowerPosition(0, 2));

        // BasicTowerShootGroup.Init(systemExecutor);
        // systemExecutor.RunFixed();

        // var z = e2.Get<DamagedBy>(t1);
        // ;


        // var towerPositions = new BackassStorage<TowerPosition>();
        // var enemyPositions = new BackassStorage<EnemyPosition>();
        // var healths = new BackassStorage<EnemyPosition>();

        // foreach (ref var tp in towerPositions)
        // {
        //     var towerPos = tp.Value;
        //     int? closest = null;
        //     var closestPos = int.MaxValue;

        //     foreach (ref var enemyPos in enemyPositions)
        //     {
        //         var dist = enemyPos.Value.DistanceSquared(towerPos);
        //         if (dist < closestPos)
        //         {
        //             closest = enemyPos.Entity;
        //             closestPos = dist;
        //         }
        //     }

        //     if (closest is null) return;
        //     closest.Value.Set(entity, new DamagedBy(data.Damage));
        // }

        var entities = new Ekaes();
        var towerPositions = new EkaesSet<TowerPosition>();
        var enemyPositions = new EkaesSet<EnemyPosition>();
        var healths = new EkaesSet<EnemyPosition>();

        var e0 = entities.Create()
            .Set(towerPositions, new TowerPosition());
        var e1 = entities.Create();

        towerPositions.Set(e0, new TowerPosition(1, 2));
        towerPositions.Set(e1, new TowerPosition(3, 4));

        ref var s1 = ref towerPositions.Get(e0);
        ref var s2 = ref towerPositions.Get(e1);

        towerPositions.Remove(e0);
        ;

        foreach (ref var z in towerPositions)
        {
            Console.WriteLine(z);
        }
    }
}

public interface IPosition
{
    int X { get; }
    int Y { get; }
}
public static class PositionExtensions
{
    public static int DistanceSquared<T1, T2>(this T1 left, T2 right)
        where T1 : unmanaged, IPosition
        where T2 : unmanaged, IPosition
    {
        var deltaX = left.X - right.X;
        var deltaY = left.Y - right.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }
}

public readonly record struct Tower;
public readonly record struct TowerPosition(int X, int Y) : IPosition;

public readonly record struct TowerCanOperate;
public readonly record struct TowerDelayPerOperation(uint DelayTicks);

public readonly record struct DamagedBy(uint Amount);


public readonly struct Enemy;
public record struct EnemyPosition(int X, int Y) : IPosition;
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

public readonly record struct Entity(int Id);
public sealed class Ekaes
{
    int Count = 0;

    public Entity Create() => new Entity(Count++);
}
public sealed class EkaesSet<T>
    where T : unmanaged
{
    const int DefaultValue = -1;
    public Span<WithHeader> Items => Dense.AsSpan(0, Count);

    int[] Sparse = new int[256];
    WithHeader[] Dense = new WithHeader[64];
    int Count = 0;

    public EkaesSet() => Sparse.AsSpan().Fill(DefaultValue);

    void EnsureSparseCapacity(int length)
    {
        var startingLength = Sparse.Length;
        while (Sparse.Length < length)
            Array.Resize(ref Sparse, Sparse.Length * 2);

        Sparse.AsSpan(startingLength).Fill(DefaultValue);
    }
    void EnsureDenseCapacity(int length)
    {
        while (Dense.Length < length)
            Array.Resize(ref Dense, Dense.Length * 2);
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
            EnsureDenseCapacity(Count);

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
        Dense[realId] = Dense[Count];
        Sparse[entity.Id] = 0;
        Sparse[Dense[Count].Entity.Id] = realId;
    }


    public Span<WithHeader>.Enumerator GetEnumerator() => Items.GetEnumerator();


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


    public static Entity Remove<T>(this Entity entity, EkaesSet<T> set)
        where T : unmanaged
    {
        set.Remove(entity);
        return entity;
    }
}
