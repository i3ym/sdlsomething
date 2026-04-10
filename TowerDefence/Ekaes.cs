namespace SdlSomething.TowerDefence;

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
