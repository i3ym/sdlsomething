namespace TowerDefence.Data;

public readonly struct Enemy;

public readonly record struct Entity(int Id)
{
    public FatEntity Fat(Ekaes world) => new(this, world);
}
public readonly record struct FatEntity(Entity Entity, Ekaes World)
{
    public static implicit operator Entity(FatEntity entity) => entity.Entity;
}

public interface IStorable { }
public interface IOnDestroyEntity : IStorable
{
    void Remove(Entity entity);
}
public sealed class Ekaes
{
    readonly Dictionary<string, IStorable> Storables = [];
    readonly Queue<int> DestroyedEntities = [];
    int MaxEntityId = 0;

    public FatEntity Entity()
    {
        if (DestroyedEntities.TryDequeue(out var existing))
            return new FatEntity(new Entity(existing), this);

        return new FatEntity(new Entity(MaxEntityId++), this);
    }

    public void Destroy(Entity entity)
    {
        foreach (var set in Storables.Values)
            if (set is IOnDestroyEntity ode)
                ode.Remove(entity);

        DestroyedEntities.Enqueue(entity.Id);
    }

    public void SetStorable(string name, IStorable storable) => Storables[name] = storable;
    public IStorable GetStorable(string name) => Storables[name];
    public bool TryGetStorable(string name, [MaybeNullWhen(false)] out IStorable storable) =>
        Storables.TryGetValue(name, out storable);

    public T Singleton<T>(string name)
        where T : IStorable, new()
    {
        if (!TryGetStorable(name, out var storable))
        {
            storable = new T();
            SetStorable(name, storable);
        }

        return (T) storable;
    }
    public T Singleton<T>()
        where T : IStorable, new() =>
        Singleton<T>(typeof(T).FullName ?? typeof(T).Name);
}
