namespace SdlSomething.TowerDefence;

public sealed class Field
{
    public List<ImmutableArray<Vec2Fixed>> Paths { get; } = [];

    public Field()
    {

    }




    public readonly record struct Line(Vec2Fixed Start, Vec2Fixed End);
}
