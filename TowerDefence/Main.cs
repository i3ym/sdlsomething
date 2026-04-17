namespace TowerDefence;

public sealed class Main
{
    public Ekaes World { get; }

    public Main()
    {
        World = new Ekaes();

        World.Singleton<Field>()
            .Paths.AddRange([
                [new Vec2Fixed(-7, -5), new Vec2Fixed(0, -5), new Vec2Fixed(0, 0)],
                // randomPath(5, 4)
            ]);

        // World.Entity()
        //     .Set(new EnemyHealth(1))
        //     // .Set(new EnemyPosition(new(0, 3)));
        //     .Set(new EnemyOnField(0, 0, 0));
        // World.Entity()
        //     .Set(new EnemyHealth(1))
        //     // .Set(new EnemyPosition(new(0, 20)));
        //     .Set(new EnemyOnField());

        World.Entity()
            .Set(new TowerPosition(new(0, 0)));


        static ImmutableArray<Vec2Fixed> randomPath(int partCount, Fixed3 maxExtrusion)
        {
            if (partCount < 2) return [];

            var builder = ImmutableArray.CreateBuilder<Vec2Fixed>(partCount + 1);
            var currentPos = randomStep(new Vec2Fixed(), maxExtrusion);
            builder.Add(currentPos);

            var returnStartIdx = (int) (partCount * 0.7f);

            for (var i = 1; i <= returnStartIdx; i++)
            {
                currentPos = randomStep(currentPos, maxExtrusion);
                builder.Add(currentPos);
            }

            for (var i = returnStartIdx + 1; i < partCount; i++)
            {
                var remainingSteps = partCount - i;
                var towardZero = new Vec2Fixed(
                    -currentPos.X / (Fixed3) remainingSteps,
                    -currentPos.Y / (Fixed3) remainingSteps
                );

                currentPos += towardZero;
                builder.Add(currentPos);
            }

            builder.Add(new Vec2Fixed(0, 0));
            return builder.ToImmutable();
        }
        static Vec2Fixed randomStep(Vec2Fixed current, Fixed3 max)
        {
            var offsetX = Fixed3.From(Random.Shared.NextDouble() * 2 - 1) * max;
            var offsetY = Fixed3.From(Random.Shared.NextDouble() * 2 - 1) * max;

            return new Vec2Fixed(current.X + offsetX, current.Y + offsetY);
        }
    }

    public void FixedTick(int tick)
    {
        EnemySystem.FixedTick(World, tick);
        EnemyFieldMovementSystem.FixedTick(World, tick);
        if (tick % 1 == 0)
            towersAttack();

        if (tick % 30 == 0)
        {
            World.Entity()
                .Set(new EnemyHealth(1))
                .Set(new EnemyOnField(0, 0, 0));
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
