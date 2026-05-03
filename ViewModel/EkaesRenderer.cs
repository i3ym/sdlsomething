namespace TowerDefence;

public static class EkaesRenderer
{
    public static void Render<T>(this Ekaes world, Standard3DInstanceDataTC storage)
        where T : unmanaged, IPosition =>
        Render(world.Component<T>(), storage);

    public static void Render<T>(this EkaesSet<T> set, Standard3DInstanceDataTC storage)
        where T : unmanaged, IPosition
    {
        var transforms = storage.Transform.GetWritableSpan(set.Count);
        var colors = storage.Color.GetWritableSpan(set.Count);

        var i = 0;
        foreach (ref readonly var pos in set)
        {
            transforms[i] = Matrix4x4.CreateTranslation(pos.Value.Position.X.ToFloat(), 0, pos.Value.Position.Y.ToFloat());
            colors[i] = new(1, 1, 1, 1);

            i++;
        }
    }
}
