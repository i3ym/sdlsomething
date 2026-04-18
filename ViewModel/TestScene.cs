namespace TowerDefence;

public sealed class TestScene
{
    readonly Renderer Renderer;

    public TestScene(Renderer renderer)
    {
        Renderer = renderer;

        // floor
        const int floorSize = 30;
        renderer.MainViewport.World.Groups.Add(new Standard3DRenderGroup(createSubdividedPlane(floorSize, 4, new(0, -.5f, 0)), null, renderer.Window));

        var cubes = new Standard3DInstanceDataTC(renderer.Device);
        renderer.MainViewport.World.Groups.Add(new Standard3DRenderGroup(PrimitiveMeshes.Cube(renderer.Device), cubes, renderer.Window));

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

            return new Standard3DMeshNCI(renderer.Device)
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

    public void Render() { }

    public bool Event(ref SDL.Event evt)
    {
        const byte leftMouseButton = 1;
        var type = (SDL.EventType) evt.Type;

        if (type == SDL.EventType.MouseButtonDown && evt.Button.Button == leftMouseButton)
            Moving = true;
        else if (type == SDL.EventType.MouseButtonUp && evt.Button.Button == leftMouseButton)
            Moving = false;
        else if (type == SDL.EventType.MouseWheel)
            Y += evt.Wheel.IntegerY;
        else if (type == SDL.EventType.MouseMotion && Moving)
        {
            X += evt.Motion.XRel / 50f;
            Z += evt.Motion.YRel / 50f;
        }

        return false;
    }

    bool Moving;
    float X, Y = 1, Z;

    public void Update()
    {
        Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(X, Y + 3, Z), new(X, Y, Z + 4), Vector3.UnitY);
    }
}
