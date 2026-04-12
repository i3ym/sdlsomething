namespace SdlSomething.TowerDefence;

public record struct EnemyOnField(int PathIndex, Fixed3 PathProgress);
public static class EnemyFieldMovementSystem
{
    public static IEnumerable<WorldTypeRegistration> GetRegistrations()
    {
        yield return WorldTypeRegistration.From<EnemyOnField>();
    }

    public static void FixedTick(Ekaes world, int tick)
    {
        var enemyPositions = world.Component<EnemyPosition>();
        var enemyFields = world.Component<EnemyOnField>();

        foreach (var iter in enemyPositions.Zip(enemyFields))
        {
            ref var pos = ref iter.Current1;
            ref var field = ref iter.Current2;

            ;
        }
    }
}
