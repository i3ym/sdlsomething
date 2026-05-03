namespace SdlSomething;

public record StandardMeshBuilderContext(
    List<Vector3> Vertices,
    List<short> Indices,
    List<Vector3>? Normals,
    List<Vector4>? Colors
);
public static class StandardMeshBuilder
{
    static void AddSpan<T>(this List<T> list, ReadOnlySpan<T> values) => list.AddRange(values);
    static void AddSpan(this List<short> list, ReadOnlySpan<short> values) => list.AddRange(values);

    public static StandardMeshBuilderContext NewNCI() => new([], [], [], []);
    public static Standard3DMeshNCI BuildNCI(this StandardMeshBuilderContext ctx, GpuDevice device)
    {
        return new Standard3DMeshNCI(device)
        {
            Vertices = { Arr = [.. ctx.Vertices] },
            Indices = { Arr = [.. ctx.Indices] },
            Colors = { Arr = [.. ctx.Colors.ThrowIfNull()] },
            Normals = { Arr = [.. ctx.Normals.ThrowIfNull()] },
        };
    }

    public static StandardMeshBuilderContext Cube(this StandardMeshBuilderContext ctx) => ctx.Cube(Vector3.One, Vector3.Zero);
    public static StandardMeshBuilderContext Cube(this StandardMeshBuilderContext ctx, Vector3 size) => ctx.Cube(size, Vector3.Zero);
    public static StandardMeshBuilderContext Cube(this StandardMeshBuilderContext ctx, Vector3 size, Vector3 position)
    {
        var (w, h, d) = size / 2;

        ctx.Vertices.AddSpan([
            position + new Vector3(-w, -h, -d), // 0
            position + new Vector3(-w, -h, +d), // 1
            position + new Vector3(+w, -h, +d), // 2
            position + new Vector3(+w, -h, -d), // 3

            position + new Vector3(-w, +h, -d), // 4
            position + new Vector3(-w, +h, +d), // 5
            position + new Vector3(+w, +h, +d), // 6
            position + new Vector3(+w, +h, -d), // 7
        ]);
        ctx.Indices.AddSpan([
            0, 4, 7, 0, 7, 3, // front
            2, 6, 5, 2, 5, 1, // back
            4, 5, 6, 4, 6, 7, // top
            3, 2, 1, 3, 1, 0, // bottom
            1, 5, 4, 1, 4, 0, // left
            7, 6, 2, 7, 2, 3, // right
        ]);

        ctx.Colors?.AddSpan([
            Vector4.One, Vector4.One, Vector4.One, Vector4.One,
            Vector4.One, Vector4.One, Vector4.One, Vector4.One,
        ]);
        ctx.Normals?.AddSpan([
            new(0, 0, -1), // 0
            new(-1, 0, 0), // 1
            new(0, 0, +1), // 2
            new(0, -1, 0), // 3

            new(0, 1, 0), // 4
            new(0, 0, 0), // 5
            new(0, 0, 0), // 6
            new(1, 0, 0), // 7
        ]);

        return ctx;
    }
}
