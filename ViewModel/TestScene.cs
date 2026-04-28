namespace TowerDefence;

public sealed class TestScene : RenderWorld
{
    public TestScene(Viewport viewport)
    {
        Add(new TopDownCameraController());

        // floor
        const int floorSize = 30;
        Add(new Standard3DRenderGroup(createSubdividedPlane(floorSize, 4, new(0, -.5f, 0)), null));

        var cubes = new Standard3DInstanceDataTC(viewport.Device);
        Add(new Standard3DRenderGroup(PrimitiveMeshes.Cube(viewport.Device), cubes));

        {
            var random = new Random(123);
            var cubeCount = 20;

            cubes.Transform.WritableData = Enumerable.Range(0, cubeCount)
                .Select(_ => Matrix4x4.CreateTranslation(
                    random.NextSingle() * floorSize,
                    random.NextSingle() * 2,
                    random.NextSingle() * floorSize
                ))
                .ToArray();

            cubes.Color.WritableData = Enumerable.Range(0, cubeCount)
                .Select(_ => new Vector4(
                    MathF.Max(random.NextSingle(), .2f),
                    MathF.Max(random.NextSingle(), .2f),
                    MathF.Max(random.NextSingle(), .2f),
                    1
                ))
                .ToArray();
        }


        Standard3DMeshNCI createSubdividedPlane(float size, int subdivisions, Vector3 offset)
        {
            var planeCount = (int) Math.Pow(4, subdivisions);
            var positions = new Vector3[planeCount * 4];
            var normals = new Vector3[planeCount * 4];
            var indices = new Int16[planeCount * 6];

            var iPerCoord = MathF.Sqrt(planeCount);
            if (iPerCoord == 0) iPerCoord = 1;

            var partSize = size / MathF.Sqrt(planeCount);
            for (var i = 0; i < planeCount; i++)
            {
                var x = ((int) (i % iPerCoord)) * partSize;
                var z = ((int) (i / iPerCoord)) * partSize;
                createPlane(positions.AsSpan(i * 4, 4), normals.AsSpan(i * 4, 4), partSize, x + offset.X, offset.Y, z + offset.Z);

                var inds = indices.AsSpan(i * 6, 6);
                inds[0] = (short) (0 + i * 4);
                inds[1] = (short) (1 + i * 4);
                inds[2] = (short) (2 + i * 4);
                inds[3] = (short) (0 + i * 4);
                inds[4] = (short) (2 + i * 4);
                inds[5] = (short) (3 + i * 4);
            }

            return new Standard3DMeshNCI(viewport.Device)
            {
                Vertices = { Arr = positions },
                Normals = { Arr = normals },
                Colors = { Arr = [.. normals.Select(_ => new Vector4(1, 1, 1, 1))] },
                Indices = { Arr = indices },
            };
        }
        static void createPlane(Span<Vector3> positions, Span<Vector3> normals, float size, float x, float y, float z)
        {
            var s = size;

            positions[0] = new(x + 0, y, z + 0);
            positions[1] = new(x + 0, y, z + s);
            positions[2] = new(x + s, y, z + s);
            positions[3] = new(x + s, y, z + 0);

            ref var a = ref positions[0];
            ref var b = ref positions[1];
            ref var c = ref positions[2];
            var edge1 = b - a;
            var edge2 = c - a;

            normals[0] = Vector3.Normalize(Vector3.Cross(edge1, edge2));
        }
    }
}
