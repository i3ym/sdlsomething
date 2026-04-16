namespace SdlSomething.Graphics;

public static class PrimitiveMeshes
{
    public static Standard3DMeshNCI Cube(GpuDevice device)
    {
        const float p = .5f;
        const float n = -p;

        return new Standard3DMeshNCI(device)
        {
            Vertices = {
                Arr = [
                    new(n, n, n), // 0
                    new(n, n, p), // 1
                    new(p, n, p), // 2
                    new(p, n, n), // 3

                    new(n, p, n), // 4
                    new(n, p, p), // 5
                    new(p, p, p), // 6
                    new(p, p, n), // 7
                ],
            },
            Normals = {
                Arr = [
                    new(0, 0, -1), // 0
                    new(-1, 0, 0), // 1
                    new(0, 0, +1), // 2
                    new(0, -1, 0), // 3

                    new(0, 1, 0), // 4
                    new(0, 0, 0), // 5
                    new(0, 0, 0), // 6
                    new(1, 0, 0), // 7
                ],
            },
            Colors = {
                Arr = [
                    Vector4.One, Vector4.One, Vector4.One, Vector4.One,
                    Vector4.One, Vector4.One, Vector4.One, Vector4.One,
                ],
            },
            Indices = {
                Arr = [
                    0, 4, 7, 0, 7, 3, // front
                    2, 6, 5, 2, 5, 1, // back
                    4, 5, 6, 4, 6, 7, // top
                    3, 2, 1, 3, 1, 0, // bottom
                    1, 5, 4, 1, 4, 0, // left
                    7, 6, 2, 7, 2, 3, // right
                ],
            },
        };
    }
    public static Standard3DMeshNCI Sphere(GpuDevice device, float radius = .5f, int rings = 16, int sectors = 32)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Vector4>();
        var indices = new List<short>();

        for (var r = 0; r < rings - 1; r++)
        {
            for (var s = 0; s < sectors - 1; s++)
            {
                var v1 = getSpherePos(r, s, rings, sectors, radius);
                var v2 = getSpherePos(r + 1, s, rings, sectors, radius);
                var v3 = getSpherePos(r + 1, s + 1, rings, sectors, radius);
                var v4 = getSpherePos(r, s + 1, rings, sectors, radius);

                addFlatTriangle(vertices, normals, colors, indices, v1, v2, v3);
                addFlatTriangle(vertices, normals, colors, indices, v1, v3, v4);
            }
        }

        return new Standard3DMeshNCI(device)
        {
            Vertices = { Arr = [.. vertices] },
            Normals = { Arr = [.. normals] },
            Colors = { Arr = [.. colors] },
            Indices = { Arr = [.. indices] },
        };

        static Vector3 getSpherePos(int r, int s, int rings, int sectors, float radius)
        {
            var x = MathF.Cos(2 * MathF.PI * s / (sectors - 1)) * MathF.Sin(MathF.PI * r / (rings - 1));
            var y = MathF.Sin(-MathF.PI / 2 + MathF.PI * r / (rings - 1));
            var z = MathF.Sin(2 * MathF.PI * s / (sectors - 1)) * MathF.Sin(MathF.PI * r / (rings - 1));
            return new Vector3(x, y, z) * radius;
        }

        static void addFlatTriangle(List<Vector3> verts, List<Vector3> normals, List<Vector4> colors, List<short> inds, Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));

            var startIdx = (short) verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            colors.Add(Vector4.One);
            colors.Add(Vector4.One);
            colors.Add(Vector4.One);

            inds.Add(startIdx);
            inds.Add((short) (startIdx + 1));
            inds.Add((short) (startIdx + 2));
        }
    }
}
