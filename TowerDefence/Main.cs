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
