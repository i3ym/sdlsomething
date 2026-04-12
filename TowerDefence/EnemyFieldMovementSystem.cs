namespace SdlSomething.TowerDefence;

public record struct EnemyOnField(int PathIndex, int PathPart, Fixed3 PathProgress);
public static class EnemyFieldMovementSystem
{
    public static void FixedTick(Ekaes world, int tick)
    {
        var field = world.Singleton<Field>();
        var enemyPositions = world.Component<EnemyPosition>();
        var enemyFields = world.Component<EnemyOnField>();

        foreach (ref var iter in enemyFields)
        {
            var entity = iter.Entity;
            ref var fieldpos = ref iter.Value;

            field.MoveForward(ref fieldpos, Fixed3.From(.05));

            var pos = field.GetPosition(fieldpos.PathIndex, fieldpos.PathPart, fieldpos.PathProgress);
            enemyPositions.Set(entity, new EnemyPosition(pos));
        }
    }
}
