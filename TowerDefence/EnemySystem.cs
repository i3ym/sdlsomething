namespace SdlSomething.TowerDefence;

public record struct EnemyHealth(int Health)
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
public record struct EnemyPosition(Fixed3 X, Fixed3 Y) : IPosition;

public static class EnemySystem
{
    public static IEnumerable<WorldTypeRegistration> GetRegistrations()
    {
        yield return WorldTypeRegistration.From<EnemyHealth>();
        yield return WorldTypeRegistration.From<EnemyPosition>();
    }

    public static void FixedTick(Ekaes world, int tick)
    {
        if (tick % 1 == 0)
            enemiesSpawn(world);
        if (tick % 5 == 0)
            enemiesMove(world);

        static void enemiesSpawn(Ekaes world)
        {
            var angle = Random.Shared.NextDouble() * Math.PI * 2;
            var pos = new EnemyPosition(Fixed3.From(Math.Cos(angle)) * 100, Fixed3.From(Math.Sin(angle)) * 100);

            world.Entity()
                .Set(new EnemyHealth(1))
                .Set(pos);
        }
        static void enemiesMove(Ekaes world)
        {
            var enemyPositions = world.Component<EnemyPosition>();
            var towerPositions = world.Component<TowerPosition>();

            foreach (ref var enemy in enemyPositions)
            {
                ref var enemyPos = ref enemy.Value;
                TowerPosition? closest = null;
                var closestDist = Fixed3.MaxValue;

                foreach (ref var towerPos in towerPositions)
                {
                    var dist = towerPos.Value.DistanceSquared(enemyPos);
                    if (dist < closestDist)
                    {
                        closest = towerPos.Value;
                        closestDist = dist;
                    }
                }

                if (closest is not { } c) continue;


                var dx = c.X - enemyPos.X;
                var dy = c.Y - enemyPos.Y;

                var cx = dx;
                var cy = dy;
                Fixed3.VecNormalize(ref cx, ref cy);

                var mult = Fixed3.From(.1f);
                enemyPos = new(enemyPos.X + cx * mult, enemyPos.Y + cy * mult);
            }
        }
    }
}
