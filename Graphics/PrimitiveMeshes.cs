namespace SdlSomething.Graphics;

public static class PrimitiveMeshes
{
    // public static Mesh<VertexPN> Cube(GpuDevice device)
    // {
    //     return new Mesh<VertexPN>(device)
    //     {
    //         VerticesArr = [
    //             // bottom
    //             new(1, 0, 0, 0, -1, 0), // 0
    //             new(1, 0, 1, 0, -1, 0), // 1
    //             new(0, 0, 1, 0, -1, 0), // 2
    //             new(0, 0, 0, 0, -1, 0), // 3

    //             // top
    //             new(0, 1, 0, 0, 1, 0), // 4
    //             new(0, 1, 1, 0, 1, 0), // 5
    //             new(1, 1, 1, 0, 1, 0), // 6
    //             new(1, 1, 0, 0, 1, 0), // 7

    //             // left
    //             new(0, 0, 1, -1, 0, 0), // 8
    //             new(0, 1, 1, -1, 0, 0), // 9
    //             new(0, 1, 0, -1, 0, 0), // 10
    //             new(0, 0, 0, -1, 0, 0), // 11

    //             // right
    //             new(1, 0, 0, 1, 0, 0), // 14
    //             new(1, 1, 0, 1, 0, 0), // 12
    //             new(1, 1, 1, 1, 0, 0), // 13
    //             new(1, 0, 1, 1, 0, 0), // 15

    //             // front
    //             new(0, 0, 0, 0, 0, 1), // 16
    //             new(0, 1, 0, 0, 0, 1), // 17
    //             new(1, 1, 0, 0, 0, 1), // 18
    //             new(1, 0, 0, 0, 0, 1), // 19

    //             // back
    //             new(1, 0, 1, 0, 0, -1), // 20
    //             new(1, 1, 1, 0, 0, -1), // 21
    //             new(0, 1, 1, 0, 0, -1), // 22
    //             new(0, 0, 1, 0, 0, -1), // 23
    //         ],
    //         IndicesArr = [
    //             0, 1, 2, 0, 2, 3,
    //             4, 5, 6, 4, 6, 7,
    //             8, 9, 10, 8, 10, 11,
    //             12, 13, 14, 12, 14, 15,
    //             16, 17, 18, 16, 18, 19,
    //             20, 21, 22, 20, 22, 23,
    //         ],
    //     };
    // }

    public static Mesh<VertexPN> Cube(GpuDevice device)
    {
        const float p = .5f;
        const float n = -p;

        return new Mesh<VertexPN>(device)
        {
            VerticesArr = [
                new(n, n, n, 0, 0, -1), // 0
                new(n, n, p, -1, 0, 0), // 1
                new(p, n, p, 0, 0, +1), // 2
                new(p, n, n, 0, -1, 0), // 3

                new(n, p, n, 0, 1, 0), // 4
                new(n, p, p, 0, 0, 0), // 5
                new(p, p, p, 0, 0, 0), // 6
                new(p, p, n, 1, 0, 0), // 7
            ],
            IndicesArr = [
                0, 4, 7, 0, 7, 3, // front
                2, 6, 5, 2, 5, 1, // back
                4, 5, 6, 4, 6, 7, // top
                3, 2, 1, 3, 1, 0, // bottom
                1, 5, 4, 1, 4, 0, // left
                7, 6, 2, 7, 2, 3, // right
            ],
        };
    }

    public static Mesh<VertexPN> Sphere(GpuDevice device, float radius = .5f, int rings = 16, int sectors = 32)
    {
        var vertices = new List<VertexPN>();
        var indices = new List<short>();

        for (var r = 0; r < rings - 1; r++)
        {
            for (var s = 0; s < sectors - 1; s++)
            {
                var v1 = getSpherePos(r, s, rings, sectors, radius);
                var v2 = getSpherePos(r + 1, s, rings, sectors, radius);
                var v3 = getSpherePos(r + 1, s + 1, rings, sectors, radius);
                var v4 = getSpherePos(r, s + 1, rings, sectors, radius);

                addFlatTriangle(vertices, indices, v1, v2, v3);
                addFlatTriangle(vertices, indices, v1, v3, v4);
            }
        }

        return new Mesh<VertexPN>(device)
        {
            VerticesArr = vertices.ToArray(),
            IndicesArr = indices.ToArray()
        };

        static Vector3 getSpherePos(int r, int s, int rings, int sectors, float radius)
        {
            var x = MathF.Cos(2 * MathF.PI * s / (sectors - 1)) * MathF.Sin(MathF.PI * r / (rings - 1));
            var y = MathF.Sin(-MathF.PI / 2 + MathF.PI * r / (rings - 1));
            var z = MathF.Sin(2 * MathF.PI * s / (sectors - 1)) * MathF.Sin(MathF.PI * r / (rings - 1));
            return new Vector3(x, y, z) * radius;
        }

        static void addFlatTriangle(List<VertexPN> verts, List<short> inds, Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));

            var startIdx = (short) verts.Count;
            verts.Add(new VertexPN(a.X, a.Y, a.Z, normal.X, normal.Y, normal.Z));
            verts.Add(new VertexPN(b.X, b.Y, b.Z, normal.X, normal.Y, normal.Z));
            verts.Add(new VertexPN(c.X, c.Y, c.Z, normal.X, normal.Y, normal.Z));

            inds.Add(startIdx);
            inds.Add((short) (startIdx + 1));
            inds.Add((short) (startIdx + 2));
        }
    }
}
