namespace SdlSomething.TowerDefence;

public sealed class Field : IStorable
{
    public List<ImmutableArray<Vec2Fixed>> Paths { get; } = [];

    public Field()
    {

    }

    public Vec2Fixed GetPosition(int index, int part, Fixed3 progress)
    {
        var path = Paths[index];

        var current = path[part];
        var next = path[part + 1];

        var normal = (next - current).Normalized();
        var prog = normal * progress;

        return current + prog;
    }

    /// <summary>
    /// Moves the entity forward by an <paramref name="amount"/> (represented in world coordinates not percentage).
    /// Returns true if reached the end.
    /// </summary>
    public bool MoveForward(ref EnemyOnField data, Fixed3 amount)
    {
        var path = Paths[data.PathIndex];
        var remaining = amount;

        while (remaining > 0)
        {
            if (data.PathPart >= path.Length - 1)
                return true;

            var current = path[data.PathPart];
            var next = path[data.PathPart + 1];

            var segmentLength = (next - current).Magnitude();
            var distanceLeftInSegment = segmentLength - data.PathProgress;

            if (remaining < distanceLeftInSegment)
            {
                data.PathProgress += remaining;
                return false;
            }
            else
            {
                remaining -= distanceLeftInSegment;
                data.PathPart++;
                data.PathProgress = 0;
            }
        }

        return data.PathPart >= path.Length - 1;
    }
}
