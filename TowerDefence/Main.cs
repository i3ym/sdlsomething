namespace SdlSomething.TowerDefence;

public sealed class Main
{
    public Ekaes World { get; }

    public Main()
    {
        World = new Ekaes([
            .. EnemySystem.GetRegistrations(),
            .. EnemyFieldMovementSystem.GetRegistrations(),
            WorldTypeRegistration.From<TowerPosition>(),
        ]);

        World.Entity()
            .Set(new EnemyHealth(1))
            .Set(new EnemyPosition(new(0, 3)));
        World.Entity()
            .Set(new EnemyHealth(1))
            .Set(new EnemyPosition(new(0, 20)));

        World.Entity()
            .Set(new TowerPosition(new(0, 0)));
    }

    public void FixedTick(int tick)
    {
        EnemySystem.FixedTick(World, tick);
        EnemyFieldMovementSystem.FixedTick(World, tick);
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
                    var dist = Vec2Fixed.DistanceSquared(enemyPos.Value.Position, towerPos.Position);
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
    Vec2Fixed Position { get; }
}
public readonly record struct Tower;
public readonly record struct TowerPosition(Vec2Fixed Position) : IPosition;

public readonly record struct TowerCanOperate;
public readonly record struct TowerDelayPerOperation(uint DelayTicks);

public readonly record struct DamagedBy(uint Amount);
