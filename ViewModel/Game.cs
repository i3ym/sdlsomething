namespace TowerDefence;

public sealed class Game
{
    readonly Renderer Renderer;

    public Game(Renderer renderer)
    {
        Renderer = renderer;

        // floor
        renderer.MainViewport.World.Groups.Add(new Standard3DRenderGroup(createSubdividedPlane(100, 4), null, renderer.Window));


        Standard3DMeshNCI createSubdividedPlane(float size, int subdivisions)
        {
            var planeCount = (int) Math.Pow(4, subdivisions);
            var positions = new Vector3[planeCount * 4];
            var normals = new Vector3[planeCount * 4];
            var indices = new Int16[planeCount * 6];

            var iPerCoord = MathF.Sqrt(planeCount);
            if (iPerCoord == 0) iPerCoord = 1;

            var planeSize = size / planeCount;
            for (var i = 0; i < planeCount; i++)
            {
                var x = ((int) (i % iPerCoord)) * planeSize;
                var z = ((int) (i / iPerCoord)) * planeSize;
                createPlane(positions.AsSpan(i * 4, 4), normals.AsSpan(i * 4, 4), planeSize, x, z);

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
        static void createPlane(Span<Vector3> positions, Span<Vector3> normals, float size, float x, float z)
        {
            var s = size;

            positions[0] = new(x + 0, Random.Shared.NextSingle() * .1f, z + 0);
            positions[1] = new(x + 0, Random.Shared.NextSingle() * .1f, z + s);
            positions[2] = new(x + s, Random.Shared.NextSingle() * .1f, z + s);
            positions[3] = new(x + s, Random.Shared.NextSingle() * .1f, z + 0);

            ref var a = ref positions[0];
            ref var b = ref positions[1];
            ref var c = ref positions[2];
            var edge1 = b - a;
            var edge2 = c - a;

            normals[0] = Vector3.Normalize(Vector3.Cross(edge1, edge2));
        }
    }

    public void Event(ref SDL.Event evt)
    {
        var type = (SDL.EventType) evt.Type;
        if (type == SDL.EventType.MouseMotion)
        {
            X += evt.Motion.XRel / 10f;
            Z += evt.Motion.YRel / 10f;
        }
    }

    float X, Z;

    public void Update()
    {
        Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(X, 4, Z), new(X, 1, Z + 4), Vector3.UnitY);
    }
}
